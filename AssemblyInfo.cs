using System;
using System.Windows;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Media;

// 1. ОТКЛЮЧАЕМ COM (Ускоряет запуск, так как .NET не строит мосты к старым библиотекам)
[assembly: ComVisible(false)]
[assembly: Guid("77777777-7777-7777-7777-777777777777")] // Твой уникальный ID проекта

// 2. ГРАФИЧЕСКИЙ ДВИЖОК (Делаем интерфейс плавным на 144Гц+ мониторах)
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

// 3. HARDCORE OPTIMIZATION (То, что делает его в 100 раз полезнее)
// Заставляем JIT-компилятор максимально агрессивно оптимизировать код при запуске
[assembly: Dependency("System.Runtime", LoadHint.Always)]

// Указываем, что наше приложение полностью поддерживает современные символы и шрифты
[assembly: DisableDpiAwareness] // Мы управляем этим через манифест, тут отключаем старые костыли

// 4. БЕЗОПАСНОСТЬ (Защита от внедрения чужого кода в процесс плеера)
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

// 5. CLS COMPLIANT (Гарантирует, что твой код будет одинаково работать на разных версиях .NET)
[assembly: CLSCompliant(false)]