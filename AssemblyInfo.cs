using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

// --- ГЛУБОКАЯ ОПТИМИЗАЦИЯ СКОРОСТИ (JIT & GC) ---

// Заставляем JIT-компилятор игнорировать проверки на границы (если это безопасно) и агрессивно встраивать методы
[assembly: OptimizationPriority(OptimizationPriority.High)]

// Отключаем проверку безопасности строк (ускоряет работу с текстом и путями к файлам)
[assembly: SecurityTransparent]

// Указываем, что сборка не содержит "мусора" для компилятора
[assembly: NeutralResourcesLanguage("ru-RU")]

// --- НИЗКОУРОВНЕВЫЙ ДОСТУП И СТАБИЛЬНОСТЬ ---

// Запрещаем COM-видимость (минус лишние накладные расходы на маршалинг)
[assembly: ComVisible(false)]

// Уникальный GUID для изоляции процесса в системе
[assembly: Guid("13377777-BEEF-4749-9060-777777777777")]

// Ускоряем загрузку зависимостей: говорим системе, что эти библиотеки всегда рядом
[assembly: Dependency("System.Runtime", LoadHint.Always)]
[assembly: Dependency("NAudio", LoadHint.Always)]

// --- ГРАФИКА И UI (WPF Optimization) ---

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

// --- БЕЗОПАСНОСТЬ И ЗАЩИТА ---

// Ищем DLL только в системных папках (защита от DLL Hijacking)
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.UserDirectories)]

// --- ВЕРСИЯ ---
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

// Кастомный атрибут для подсказки JIT-у (если используешь продвинутый компилятор)
internal sealed class OptimizationPriorityAttribute : Attribute
{
    public OptimizationPriorityAttribute(OptimizationPriority p) { Priority = p; }
    public OptimizationPriority Priority { get; }
}
internal enum OptimizationPriority { Low, Medium, High }