using System.Globalization;

namespace ClearAIText.App.Services;

/// <summary>
/// Service managing dynamic UI localization and translations.
/// Supports English and Russian with runtime language switching and system language auto-detection.
/// </summary>
public sealed class LocalizationService
{
    private string _currentLanguage = "ru";

    public event Action? LanguageChanged;

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            string normalized = NormalizeLanguageCode(value);
            if (_currentLanguage != normalized)
            {
                _currentLanguage = normalized;
                LanguageChanged?.Invoke();
            }
        }
    }

    public static string DetectSystemLanguage()
    {
        try
        {
            string currentUi = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (string.Equals(currentUi, "ru", StringComparison.OrdinalIgnoreCase))
            {
                return "ru";
            }

            string installedUi = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
            if (string.Equals(installedUi, "ru", StringComparison.OrdinalIgnoreCase))
            {
                return "ru";
            }
        }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentException)
        {
            // Fallback safely to English on error
        }

        return "en";
    }

    private static string NormalizeLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "ru";
        }

        if (code.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
        {
            return "ru";
        }

        return "en";
    }

    public string this[string key] => GetString(key);

    public string GetString(string key, params object[] args)
    {
        var dict = _currentLanguage == "ru" ? RussianStrings : EnglishStrings;

        if (!dict.TryGetValue(key, out string? value))
        {
            if (!EnglishStrings.TryGetValue(key, out value))
            {
                return $"[{key}]";
            }
        }

        if (args.Length > 0)
        {
            try
            {
                return string.Format(CultureInfo.InvariantCulture, value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        return value;
    }

    private static readonly Dictionary<string, string> RussianStrings = new(StringComparer.Ordinal)
    {
        // Navigation & General
        ["AppName"] = "Clear AI Text",
        ["NavMonitor"] = "Мониторинг",
        ["NavSandbox"] = "Песочница",
        ["NavSettings"] = "Настройки",
        ["NavAbout"] = "О программе",
        ["NavExit"] = "Выход",
        ["StatusMonitoringActive"] = "Мониторинг активен",
        ["StatusMonitoringPaused"] = "Мониторинг приостановлен",

        // Monitor Page
        ["MonitorTitle"] = "Мониторинг буфера обмена",
        ["MonitorSubtitle"] = "Автоматический перехват и безопасная нормализация специфической ИИ-типографики в реальном времени.",
        ["StatTotalEvents"] = "Всего событий",
        ["StatCleaned"] = "Очищено",
        ["StatIgnored"] = "Пропущено",
        ["StatAvgTime"] = "Ср. время",
        ["StatLastActivity"] = "Посл. активность",
        ["ButtonClearHistory"] = "Очистить историю",
        ["OperationsLogHeader"] = "Журнал операций",
        ["EmptyLogTitle"] = "История пока пуста",
        ["EmptyLogSubtitle"] = "Скопируйте текст из чата ИИ или браузера. Приложение автоматически очистит нестандартную типографику.",
        ["ButtonOriginal"] = "Оригинал",
        ["ButtonCopy"] = "Копировать",
        ["LabelOriginal"] = "Оригинал:",
        ["LabelCleaned"] = "Нормализовано:",
        ["SystemClipboard"] = "Системный буфер",

        // Sandbox Page
        ["SandboxTitle"] = "Интерактивная песочница",
        ["SandboxSubtitle"] = "Проверяйте и тестируйте правила нормализации текста в реальном времени.",
        ["SandboxModeLabel"] = "Режим:",
        ["SandboxModeTier1"] = "Безопасная типографика (Tier 1)",
        ["SandboxModeAggressive"] = "Агрессивная очистка (Tier 1 + 2)",
        ["SandboxModeCustomOnly"] = "Только пользовательские правила",
        ["SandboxModeCurrent"] = "По текущим настройкам",
        ["ButtonPaste"] = "Вставить",
        ["ButtonClear"] = "Очистить",
        ["ButtonSwap"] = "Как ввод",
        ["ButtonCopyResult"] = "Скопировать",
        ["ButtonCopied"] = "Скопировано!",
        ["InputHeader"] = "Исходный текст",
        ["OutputHeader"] = "Результат",
        ["InputPlaceholder"] = "Введите или вставьте текст с типографикой ИИ, тире, кавычками, пробелами или Markdown...",
        ["OutputPlaceholder"] = "Здесь появится очищенный текст...",
        ["StatInputLength"] = "{0} симв., {1} стр.",
        ["StatOutputLength"] = "{0} симв. ({1:+0;-0;0})",
        ["StatOutputLengthZero"] = "0 симв.",
        ["StatTimeFormat"] = "{0:F2} мс",
        ["StatReplacementsFormat"] = "Замен: {0}",
        ["AnalysisResultsTitle"] = "Результаты анализа и примененные правила",
        ["MonitoringDisabledHint"] = "Глобальный мониторинг отключен в настройках, но песочница работает независимо.",
        ["NoModificationsNeeded"] = "Текст не содержит специфических символов (изменений не требуется).",
        ["EnterTextHint"] = "Введите или вставьте текст для анализа",
        ["HomoglyphsFoundFormat"] = "Обнаружено смешение алфавитов (потенциальные гомоглифы: {0} шт. в словах: {1}). Текст сохранен без авто-замены для безопасности.",

        // Settings Page - Titles & Groups
        ["SettingsTitle"] = "Настройки Clear AI Text",
        ["SettingsSubtitle"] = "Управление правилами очистки, пользовательскими заменами, исключениями процессов и параметрами системы.",
        ["GroupAppearance"] = "Внешний вид и язык",
        ["GroupAppearanceDesc"] = "Настройка темы оформления и языка интерфейса приложения.",
        ["ThemeHeader"] = "Тема оформления",
        ["ThemeSystem"] = "Как в системе",
        ["ThemeLight"] = "Светлая",
        ["ThemeDark"] = "Темная",
        ["LanguageHeader"] = "Язык интерфейса",
        ["LanguageRussian"] = "Русский (Russian)",
        ["LanguageEnglish"] = "English",

        ["ToggleMaster"] = "Нормализация текста",
        ["ToggleMasterDesc"] = "Главный переключатель конвейера нормализации. При выключении текст проходит без изменений.",
        ["GroupTier1"] = "Уровень 1: Безопасная нормализация (Tier 1)",
        ["GroupTier1Desc"] = "Правила, сохраняющие семантику и структуру текста. Рекомендуются к постоянному включению.",
        ["ToggleDashes"] = "Замена длинных тире на дефис",
        ["ToggleQuotes"] = "Замена типографских кавычек на прямые",
        ["ToggleSpaces"] = "Нормализация неразрывных и экзотических пробелов",
        ["ToggleInvisible"] = "Удаление невидимых символов и BOM",

        ["GroupCustomRules"] = "Пользовательские правила",
        ["GroupCustomRulesDesc"] = "Добавляйте и редактируйте свои символы или шаблоны для автоматического удаления либо замены.",
        ["RulePatternHeader"] = "Символ, текст или Regex-шаблон",
        ["RulePatternPlaceholder"] = "Например: ★, [AI], \\s{2,}",
        ["RuleActionHeader"] = "Действие",
        ["RuleActionDelete"] = "Удалять символ / текст",
        ["RuleActionReplace"] = "Заменять на другое значение",
        ["RuleReplacementHeader"] = "Заменять на (текст / символ)",
        ["RuleReplacementPlaceholder"] = "Например: пробел, дефис или текст",
        ["RuleIsRegex"] = "Использовать регулярное выражение (Regex)",
        ["RuleRegexHint"] = "Флаг Regex включает поиск по регулярным выражениям вместо точного совпадения текста.",
        ["ButtonAddRule"] = "Добавить правило",
        ["ButtonSaveRule"] = "Сохранить изменения",
        ["ButtonCancelRule"] = "Отмена",
        ["CustomRulesListHeader"] = "Список пользовательских правил:",
        ["EmptyCustomRulesText"] = "Пользовательские правила еще не добавлены.",
        ["ButtonEdit"] = "Изменить",
        ["ButtonDelete"] = "Удалить",
        ["ActionDeleteBadge"] = "Удаление",

        ["GroupTier2"] = "Уровень 2: Деструктивная очистка (Tier 2)",
        ["GroupTier2Desc"] = "Опциональные правила, удаляющие определенные типы контента.",
        ["ToggleDiacritics"] = "Удаление диакритических знаков (акцентов)",
        ["ToggleEmojis"] = "Удаление эмодзи",
        ["ToggleMarkdown"] = "Очистка Markdown-разметки",

        ["GroupExclusions"] = "Исключения приложений",
        ["GroupExclusionsDesc"] = "Приложения, при копировании из которых очистка буфера обмена НЕ выполняется (например, менеджеры паролей).",
        ["ProcessPlaceholder"] = "Имя процесса (например: KeePass, Bitwarden)",
        ["ButtonAddProcess"] = "Добавить",
        ["ButtonPickRunning"] = "Выбрать из запущенных...",
        ["RunningAppsHeader"] = "Запущенные приложения",
        ["RunningAppsDesc"] = "Нажмите на приложение, чтобы добавить его в исключения:",
        ["EmptyRunningAppsText"] = "Нет доступных оконных приложений.",
        ["EmptyExcludedProcessesText"] = "Список исключений пуст.",

        ["GroupSystem"] = "Интеграция с системой",
        ["ToggleStartWithWindows"] = "Автозапуск при старте Windows",
        ["ToggleMinimizeToTray"] = "Сворачивать в системный трей при закрытии",
        ["ToggleMinimizeToTrayOff"] = "Закрывать приложение",
        ["ToggleMinimizeToTrayOn"] = "Сворачивать в трей",

        // Validations & Prompts
        ["ValidationEmptyProcess"] = "Введите имя процесса (например: KeePass, notepad) или выберите из запущенных.",
        ["ValidationInvalidProcess"] = "Введите корректное имя процесса.",
        ["ValidationDuplicateProcess"] = "Процесс «{0}» уже есть в списке исключений.",
        ["ValidationEmptyRule"] = "Введите образец текста или регулярное выражение для правила.",
        ["ValidationInvalidRegex"] = "Некорректный синтаксис регулярного выражения.",

        // About Page
        ["AboutTitle"] = "Clear AI Text",
        ["AboutSubtitle"] = "Высокопроизводительный фоновый сервис нормализации и безопасной очистки типографики буфера обмена Windows.",
        ["AboutSecurityTitle"] = "100% Приватность и Безопасность",
        ["AboutSecurityDesc"] = "Полностью локальная обработка. Нулевая телеметрия, отсутствие сетевых запросов и автоматический пропуск защищенных менеджеров паролей.",
        ["AboutPerfTitle"] = "Нулевая задержка (Sub-millisecond)",
        ["AboutPerfDesc"] = "Оптимизированный C# 13 / .NET 10 пайплайн с нулевыми аллокациями и нативной Win32 интеграцией.",
        ["AboutEngineTitle"] = "Трехуровневая модель Unicode",
        ["AboutEngineDesc"] = "Строгое разделение безопасной типографики, деструктивной очистки и интерактивного обнаружения омоглифов.",
        ["AboutTechStackHeader"] = "Технический стек и архитектура",
        ["AboutVersionFormat"] = "Версия 1.0.0 (x64 / ARM64)",

        // Tray Context Menu
        ["TrayShowWindow"] = "Открыть",
        ["TraySandbox"] = "Песочница",
        ["TraySettings"] = "Настройки",
        ["TrayMonitoring"] = "Нормализация",
        ["TrayExit"] = "Выход",
        ["ToggleEnabled"] = "Включено",
        ["ToggleDisabled"] = "Отключено",
        ["ToggleOn"] = "Включен",
        ["ToggleOff"] = "Выключен"
    };

    private static readonly Dictionary<string, string> EnglishStrings = new(StringComparer.Ordinal)
    {
        // Navigation & General
        ["AppName"] = "Clear AI Text",
        ["NavMonitor"] = "Monitoring",
        ["NavSandbox"] = "Sandbox",
        ["NavSettings"] = "Settings",
        ["NavAbout"] = "About",
        ["NavExit"] = "Exit",
        ["StatusMonitoringActive"] = "Monitoring active",
        ["StatusMonitoringPaused"] = "Monitoring paused",

        // Monitor Page
        ["MonitorTitle"] = "Clipboard Monitoring",
        ["MonitorSubtitle"] = "Automatic interception and safe normalization of AI typography in real time.",
        ["StatTotalEvents"] = "Total Events",
        ["StatCleaned"] = "Cleaned",
        ["StatIgnored"] = "Skipped",
        ["StatAvgTime"] = "Avg Time",
        ["StatLastActivity"] = "Last Activity",
        ["ButtonClearHistory"] = "Clear History",
        ["OperationsLogHeader"] = "Operations Log",
        ["EmptyLogTitle"] = "History is currently empty",
        ["EmptyLogSubtitle"] = "Copy text from an AI chat or browser. The app will automatically sanitize typography.",
        ["ButtonOriginal"] = "Original",
        ["ButtonCopy"] = "Copy",
        ["LabelOriginal"] = "Original:",
        ["LabelCleaned"] = "Normalized:",
        ["SystemClipboard"] = "System Clipboard",

        // Sandbox Page
        ["SandboxTitle"] = "Interactive Sandbox",
        ["SandboxSubtitle"] = "Test and verify text normalization rules in real time.",
        ["SandboxModeLabel"] = "Mode:",
        ["SandboxModeTier1"] = "Safe typography (Tier 1)",
        ["SandboxModeAggressive"] = "Aggressive cleaning (Tier 1 + 2)",
        ["SandboxModeCustomOnly"] = "Custom rules only",
        ["SandboxModeCurrent"] = "According to settings",
        ["ButtonPaste"] = "Paste",
        ["ButtonClear"] = "Clear",
        ["ButtonSwap"] = "As input",
        ["ButtonCopyResult"] = "Copy result",
        ["ButtonCopied"] = "Copied!",
        ["InputHeader"] = "Source text",
        ["OutputHeader"] = "Result",
        ["InputPlaceholder"] = "Type or paste text with AI typography, dashes, quotes, spaces, or Markdown...",
        ["OutputPlaceholder"] = "Sanitized text will appear here...",
        ["StatInputLength"] = "{0} chars, {1} lines",
        ["StatOutputLength"] = "{0} chars ({1:+0;-0;0})",
        ["StatOutputLengthZero"] = "0 chars",
        ["StatTimeFormat"] = "{0:F2} ms",
        ["StatReplacementsFormat"] = "Replacements: {0}",
        ["AnalysisResultsTitle"] = "Analysis results and applied rules",
        ["MonitoringDisabledHint"] = "Global clipboard monitoring is disabled in settings, but Sandbox works independently.",
        ["NoModificationsNeeded"] = "Text does not contain specific characters (no changes needed).",
        ["EnterTextHint"] = "Enter or paste text to analyze",
        ["HomoglyphsFoundFormat"] = "Mixed alphabets detected (potential homoglyphs: {0} in words: {1}). Text preserved without auto-replacement for safety.",

        // Settings Page - Titles & Groups
        ["SettingsTitle"] = "Clear AI Text Settings",
        ["SettingsSubtitle"] = "Manage cleaning rules, custom replacements, process exclusions, and system options.",
        ["GroupAppearance"] = "Appearance & Language",
        ["GroupAppearanceDesc"] = "Customize application theme and interface language.",
        ["ThemeHeader"] = "App Theme",
        ["ThemeSystem"] = "System default",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["LanguageHeader"] = "Interface Language",
        ["LanguageRussian"] = "Русский (Russian)",
        ["LanguageEnglish"] = "English",

        ["ToggleMaster"] = "Text Normalization",
        ["ToggleMasterDesc"] = "Master switch for text normalization pipeline. When disabled, clipboard text passes untouched.",
        ["GroupTier1"] = "Tier 1: Safe Normalization (Tier 1)",
        ["GroupTier1Desc"] = "Rules preserving semantic structure. Recommended to stay enabled.",
        ["ToggleDashes"] = "Normalize em/en dashes to hyphen",
        ["ToggleQuotes"] = "Normalize curly/typographic quotes to straight",
        ["ToggleSpaces"] = "Normalize non-breaking and exotic spaces",
        ["ToggleInvisible"] = "Remove invisible characters and BOM",

        ["GroupCustomRules"] = "Custom Rules",
        ["GroupCustomRulesDesc"] = "Add and edit your own words, characters, or Regex patterns to remove or replace.",
        ["RulePatternHeader"] = "Pattern (text / character / Regex)",
        ["RulePatternPlaceholder"] = "For example: ★, [AI], \\s{2,}",
        ["RuleActionHeader"] = "Action",
        ["RuleActionDelete"] = "Remove symbol / text",
        ["RuleActionReplace"] = "Replace with another value",
        ["RuleReplacementHeader"] = "Replace with (text / character)",
        ["RuleReplacementPlaceholder"] = "For example: space, hyphen or text",
        ["RuleIsRegex"] = "Use Regular Expression (Regex)",
        ["RuleRegexHint"] = "Regex flag enables regular expression matching instead of literal text match.",
        ["ButtonAddRule"] = "Add Rule",
        ["ButtonSaveRule"] = "Save Changes",
        ["ButtonCancelRule"] = "Cancel",
        ["CustomRulesListHeader"] = "Custom Rules List:",
        ["EmptyCustomRulesText"] = "No custom rules added yet.",
        ["ButtonEdit"] = "Edit",
        ["ButtonDelete"] = "Delete",
        ["ActionDeleteBadge"] = "Remove",

        ["GroupTier2"] = "Tier 2: Destructive Cleaning (Tier 2)",
        ["GroupTier2Desc"] = "Optional rules that strip specific types of content.",
        ["ToggleDiacritics"] = "Strip diacritics (accents)",
        ["ToggleEmojis"] = "Strip emojis",
        ["ToggleMarkdown"] = "Strip Markdown formatting",

        ["GroupExclusions"] = "Application Exclusions",
        ["GroupExclusionsDesc"] = "Applications excluded from clipboard normalization (e.g., password managers).",
        ["ProcessPlaceholder"] = "Process name (e.g.: KeePass, Bitwarden)",
        ["ButtonAddProcess"] = "Add",
        ["ButtonPickRunning"] = "Pick running app...",
        ["RunningAppsHeader"] = "Running Applications",
        ["RunningAppsDesc"] = "Click an application to add it to exclusions:",
        ["EmptyRunningAppsText"] = "No active window applications found.",
        ["EmptyExcludedProcessesText"] = "Exclusions list is empty.",

        ["GroupSystem"] = "System Integration",
        ["ToggleStartWithWindows"] = "Start with Windows",
        ["ToggleMinimizeToTray"] = "Minimize to system tray on close",
        ["ToggleMinimizeToTrayOff"] = "Close application",
        ["ToggleMinimizeToTrayOn"] = "Minimize to tray",

        // Validations & Prompts
        ["ValidationEmptyProcess"] = "Enter process name (e.g.: KeePass, notepad) or pick from running apps.",
        ["ValidationInvalidProcess"] = "Enter a valid process name.",
        ["ValidationDuplicateProcess"] = "Process \"{0}\" is already in exclusions list.",
        ["ValidationEmptyRule"] = "Enter search text or regular expression for rule.",
        ["ValidationInvalidRegex"] = "Invalid regular expression syntax.",

        // About Page
        ["AboutTitle"] = "Clear AI Text",
        ["AboutSubtitle"] = "High-performance background Windows typography sanitizer and clipboard normalizer.",
        ["AboutSecurityTitle"] = "100% Privacy & Security",
        ["AboutSecurityDesc"] = "Strictly local processing. Zero telemetry, zero network requests, automatic exclusion of password managers.",
        ["AboutPerfTitle"] = "Zero Latency (Sub-millisecond)",
        ["AboutPerfDesc"] = "Optimized C# 13 / .NET 10 pipeline with zero heap thrashing and native Win32 clipboard integration.",
        ["AboutEngineTitle"] = "Three-Tier Unicode Architecture",
        ["AboutEngineDesc"] = "Strict separation of safe typography, destructive cleaning, and interactive homoglyph inspection.",
        ["AboutTechStackHeader"] = "Technical Stack & Architecture",
        ["AboutVersionFormat"] = "Version 1.0.0 (x64 / ARM64)",

        // Tray Context Menu
        ["TrayShowWindow"] = "Open",
        ["TraySandbox"] = "Sandbox",
        ["TraySettings"] = "Settings",
        ["TrayMonitoring"] = "Normalization",
        ["TrayExit"] = "Exit",
        ["ToggleEnabled"] = "Enabled",
        ["ToggleDisabled"] = "Disabled",
        ["ToggleOn"] = "Enabled",
        ["ToggleOff"] = "Disabled"
    };
}
