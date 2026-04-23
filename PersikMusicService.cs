using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using NAudio.Wave;
using PersikMusic.Models;
using PersikMusic.Core; // Твой Hi-Fi движок

namespace PersikMusic.Services
{
    public class PersikMusicService : IDisposable
    {
        private DriveService? _driveService;
        private IWavePlayer? _output;
        private WaveStream? _reader;

        public void Initialize()
        {
            try
            {
                string keyPath = "suda key.json";

                if (!File.Exists(keyPath))
                {
                    MessageBox.Show($"Файл ключа не найден: {Path.GetFullPath(keyPath)}");
                    return;
                }

                using (var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read))
                {
#pragma warning disable CS0618
                    var credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(DriveService.Scope.DriveReadonly);
#pragma warning restore CS0618

                    _driveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "OpenPersikMusic"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации Drive: {ex.Message}");
            }
        }

        public async void PlayStream(string driveId)
        {
            if (_driveService == null) return;

            // Полная остановка перед новым треком
            Stop();

            try
            {
                var request = _driveService.Files.Get(driveId);
                var memStream = new MemoryStream();
                await request.DownloadAsync(memStream);
                memStream.Position = 0;

                // Создаем ридер. Используем WaveFileReader для WAV или RawSourceWaveStream для FPSC
                _reader = new WaveFileReader(memStream);
                
                // Инициализируем твой Hi-Fi движок
                var sampleProvider = _reader.ToSampleProvider();
                var hifiEngine = new PersikHiFiEngine(sampleProvider);

                // WASAPI Exclusive для Hi-Fi (минимальная задержка)
                _output = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, 100);
                _output.Init(hifiEngine);
                _output.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения: {ex.Message}");
                Stop();
            }
        }

        public async Task<List<SongItem>> GetSongsFromFolder(string folderId)
        {
            var songs = new List<SongItem>();
            if (_driveService == null) return songs;

            try
            {
                var request = _driveService.Files.List();
                request.Q = $"'{folderId}' in parents and trashed = false";
                request.Fields = "files(id, name)";

                var result = await request.ExecuteAsync();
                if (result.Files != null)
                {
                    foreach (var file in result.Files)
                    {
                        songs.Add(new SongItem { DriveId = file.Id, Title = file.Name });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось получить список песен: {ex.Message}");
            }
            return songs;
        }

        // --- СОСТОЯНИЕ ---
        public bool IsPlaying() => _output != null && _output.PlaybackState == PlaybackState.Playing;
        public bool HasAudioLoaded() => _reader != null;

        // --- УПРАВЛЕНИЕ ---
        public void Pause()
        {
            if (_output != null)
            {
                _output.Pause();
                // Фикс "зацикливания": Stop сбрасывает буферы аудиокарты, 
                // но так как мы не обнуляем _reader, позиция сохраняется.
                _output.Stop();
            }
        }

        public void Resume() => _output?.Play();

        public void Stop()
        {
            // 1. Сначала останавливаем железку
            if (_output != null) _output.Stop();

            // 2. Копируем ссылки и ОБНУЛЯЕМ оригиналы (важно для потокобезопасности таймера)
            var outToDispose = _output;
            var readerToDispose = _reader;
            _output = null;
            _reader = null;

            // 3. Чистим ресурсы
            outToDispose?.Dispose();
            readerToDispose?.Dispose();
        }

        public void SetVolume(float v)
        {
            if (_output != null) _output.Volume = v;
        }

        public double GetProgress()
        {
            // Работаем с локальной копией, чтобы избежать NullReference
            var r = _reader;
            if (r == null || r.TotalTime.TotalSeconds <= 0) return 0;

            try
            {
                return (r.CurrentTime.TotalSeconds / r.TotalTime.TotalSeconds) * 100;
            }
            catch { return 0; }
        }

        public void Seek(double pct)
        {
            var r = _reader;
            if (r != null && r.TotalTime.TotalSeconds > 0)
            {
                r.CurrentTime = TimeSpan.FromSeconds(r.TotalTime.TotalSeconds * (pct / 100));
            }
        }

        public void Dispose() => Stop();
    }
}