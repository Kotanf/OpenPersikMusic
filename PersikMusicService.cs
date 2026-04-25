using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PersikMusic.Core;
using PersikMusic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;

namespace PersikMusic.Services
{
    [SupportedOSPlatform("windows")]
    public class PersikMusicService : IDisposable
    {
        private DriveService? _driveService;
        private IWavePlayer? _output;
        private WaveStream? _reader;

        public void Initialize()
        {
            try
            {
                string keyPath = "test.json";
                if (!File.Exists(keyPath))
                {
                    MessageBox.Show($"Файл ключа не найден: {Path.GetFullPath(keyPath)}");
                    return;
                }

                var credential = CredentialFactory.FromFile<ServiceAccountCredential>(keyPath)
                    .ToGoogleCredential()
                    .CreateScoped(DriveService.Scope.DriveReadonly);

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "PersikMusicHiFi"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка Drive: {ex.Message}");
            }
        }

        public async void PlayStream(string driveId)
        {
            if (_driveService == null) return;
            Stop();

            try
            {
                var request = _driveService.Files.Get(driveId);
                var memStream = new MemoryStream();
                await request.DownloadAsync(memStream);
                memStream.Position = 0;
                _reader = new StreamMediaFoundationReader(memStream);

                var sampleProvider = _reader.ToSampleProvider();
                var hifiEngine = new PersikHiFiEngine(sampleProvider);

                var resampler = new WdlResamplingSampleProvider(hifiEngine, 48000);

                _output = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, 100);
                _output.Init(resampler);
                _output.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка Hi-Fi: {ex.Message}");
                Stop();
            }
        }

        public void Pause()
        {
            if (_output != null && _output.PlaybackState == PlaybackState.Playing)
            {
                _output.Stop();
            }
        }

        public void Resume()
        {
            if (_output != null && _output.PlaybackState != PlaybackState.Playing)
            {
                _output.Play();
            }
        }

        public void Stop()
        {
            if (_output != null)
            {
                _output.Stop();
                _output.Dispose();
                _output = null;
            }

            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }

        public bool IsPlaying() => _output != null && _output.PlaybackState == PlaybackState.Playing;
        public bool HasAudioLoaded() => _reader != null;
        public void SetVolume(float v) { if (_output != null) _output.Volume = v; }

        public double GetProgress()
        {
            if (_reader == null || _reader.TotalTime.TotalSeconds <= 0) return 0;
            return (_reader.CurrentTime.TotalSeconds / _reader.TotalTime.TotalSeconds) * 100;
        }

        public void Seek(double pct)
        {
            if (_reader != null)
                _reader.CurrentTime = TimeSpan.FromSeconds(_reader.TotalTime.TotalSeconds * (pct / 100));
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
                foreach (var f in result.Files) songs.Add(new SongItem { DriveId = f.Id, Title = f.Name });
            }
            catch { }
            return songs;
        }

        public void Dispose() => Stop();
    }
}