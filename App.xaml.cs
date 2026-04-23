using System.Windows;
using PersikMusic.Core; // Обязательно добавь этот using, чтобы App видел папку Core

namespace PersikMusic
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Магия начинается здесь:
            // Проверяем обновления в фоновом режиме, чтобы не вешать запуск плеера
            await PersikUpdater.CheckForUpdates();
        }
    }
}