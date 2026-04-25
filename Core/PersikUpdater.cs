using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows; // Если используешь WPF или WinForms для окошка

namespace PersikMusic.Core
{
    public static class PersikUpdater
    {
        // Ссылка на текстовый файл на твоем диске/сервере, где написана только версия (например, 10.0.600)
        private const string VersionUrl = "https://googlne.com";
        // Прямая ссылка на сам установщик .exe
        private const string DownloadUrl = "https://yandex.ru";

        public static string CurrentVersion = "10.0.810";

        public static async Task CheckForUpdates()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 1. Получаем номер свежей версии
                    string latestVersion = (await client.GetStringAsync(VersionUrl)).Trim();

                    if (latestVersion != CurrentVersion)
                    {
                        var result = MessageBox.Show(
                            $"Доступна новая версия {latestVersion}! Скачать и обновить?",
                            "Обновление Открытой Персик Музыки",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            await StartUpdate(client);
                        }
                    }
                }
            }
            catch { /* Если нет инета, просто молчим и работаем дальше */ }
        }

        private static async Task StartUpdate(HttpClient client)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "PersikSetup.exe");

            // 2. Качаем новый инсталлятор
            byte[] fileBytes = await client.GetByteArrayAsync(DownloadUrl);
            await File.WriteAllBytesAsync(tempPath, fileBytes);

            // 3. Запускаем установщик и закрываем текущий плеер
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });

            Application.Current.Shutdown(); // Закрываем плеер, чтобы инсталлятор мог его заменить
        }
    }
}