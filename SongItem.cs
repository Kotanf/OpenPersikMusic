using System.Windows.Media;

namespace PersikMusic.Models
{
    public class SongItem
    {
        public string DriveId { get; set; } = "";
        public string Title { get; set; } = "";
        public bool IsFavorite { get; set; }

        // Цвет сердечка в UI
        public Brush FavoriteBrush => IsFavorite ? Brushes.Tomato : Brushes.DimGray;
    }
}