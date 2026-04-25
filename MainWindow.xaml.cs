using PersikMusic.Models;
using PersikMusic.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PersikMusic
{
    public partial class MainWindow : Window
    {
        private readonly PersikMusicService _audioService = new();
        private readonly MongoService _dbService = new();
        private readonly DispatcherTimer _timer = new();
        private User? _currentUser;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            _audioService.Initialize();

            // Имитация входа пользователя
            LoadUserAsync();

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += (s, e) =>
            {
                if (!_isDragging && _audioService.GetProgress() > 0)
                    TimelineSlider.Value = _audioService.GetProgress();
            };
            _timer.Start();
        }

        private async void LoadUserAsync()
        {
            _currentUser = await _dbService.GetOrCreateUser("Persik");
        }

        private async void Folder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string folderId)
            {
                try
                {
                    CategoryTitle.Text = btn.Content.ToString()?.ToUpper();
                    // Получаем песни из конкретной папки
                    var songs = await _audioService.GetSongsFromFolder(folderId);

                    if (_currentUser != null)
                    {
                        foreach (var s in songs)
                            s.IsFavorite = _currentUser.FavoriteDriveIds.Contains(s.DriveId);
                    }

                    SongListView.ItemsSource = songs;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка доступа к папке: {ex.Message}\nВозможно папка удалена, скоро выйдет обновление, где это удалю.");
                }
            }
        }

        private void Song_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongListView.SelectedItem is SongItem song)
            {
                // Метод PlayStream теперь принимает DriveId
                _audioService.PlayStream(song.DriveId);

                // Обновляем текст в плеере (если есть элементы с именами)
                // CurrentTrackTitle.Text = song.Title; 
                PlayPauseBtn.Content = "⏸️";
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверяем, выбрана ли песня вообще
            if (SongListView.SelectedItem is not SongItem selectedSong)
            {
                return;
            }

            // 2. Определяем текущее состояние через сервис (это надежнее, чем текст кнопки)
            bool isPlaying = _audioService.IsPlaying();

            if (isPlaying)
            {
                // Если играет — ставим на паузу
                _audioService.Pause();
                PlayPauseBtn.Content = "▶";
            }
            else
            {
                // Если на паузе — продолжаем. 
                // Если ничего не было загружено (первый запуск) — вызываем PlayStream
                if (_audioService.HasAudioLoaded())
                {
                    _audioService.Resume();
                }
                else
                {
                    _audioService.PlayStream(selectedSong.DriveId);
                }

                PlayPauseBtn.Content = "⏸";
            }
        }

        private async void ToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null) return;
            if (sender is Button btn && btn.Tag is SongItem song)
            {
                if (_currentUser.FavoriteDriveIds.Contains(song.DriveId))
                    _currentUser.FavoriteDriveIds.Remove(song.DriveId);
                else
                    _currentUser.FavoriteDriveIds.Add(song.DriveId);

                await _dbService.UpdateFavorites(_currentUser.Id, _currentUser.FavoriteDriveIds);
                song.IsFavorite = !song.IsFavorite;
                SongListView.Items.Refresh();
            }
        }

        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => _audioService.SetVolume((float)e.NewValue);
        private void Timeline_DragStarted(object sender, EventArgs e) => _isDragging = true;
        private void Timeline_DragCompleted(object sender, EventArgs e) { _audioService.Seek(TimelineSlider.Value); _isDragging = false; }
        private void Playlists_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Функция в разработке!");
    }
}