namespace BTAudioDriver.Localization;

public static class Strings
{
    private static Language _currentLanguage = Language.Russian;
    private static readonly Dictionary<string, Dictionary<Language, string>> _strings = new();

    public static event EventHandler? LanguageChanged;

    static Strings()
    {
        InitializeStrings();
    }

    public static Language CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    public static string Get(string key)
    {
        if (_strings.TryGetValue(key, out var translations))
        {
            if (translations.TryGetValue(_currentLanguage, out var value))
                return value;
            if (translations.TryGetValue(Language.English, out var fallback))
                return fallback;
        }
        return key;
    }

    public static string AppName => Get("AppName");
    public static string AppDescription => Get("AppDescription");

    public static string MainWindow_Title => Get("MainWindow.Title");
    public static string MainWindow_Subtitle => Get("MainWindow.Subtitle");
    public static string MainWindow_CurrentMode => Get("MainWindow.CurrentMode");
    public static string MainWindow_Music => Get("MainWindow.Music");
    public static string MainWindow_Calls => Get("MainWindow.Calls");
    public static string MainWindow_MusicDescription => Get("MainWindow.MusicDescription");
    public static string MainWindow_CallsDescription => Get("MainWindow.CallsDescription");
    public static string MainWindow_Settings => Get("MainWindow.Settings");
    public static string MainWindow_Diagnostics => Get("MainWindow.Diagnostics");
    public static string MainWindow_MinimizeToTray => Get("MainWindow.MinimizeToTray");
    public static string MainWindow_Help => Get("MainWindow.Help");

    public static string Device_NotConnected => Get("Device.NotConnected");
    public static string Device_Connected => Get("Device.Connected");

    public static string Mode_Music => Get("Mode.Music");
    public static string Mode_Calls => Get("Mode.Calls");
    public static string Mode_Unknown => Get("Mode.Unknown");
    public static string Mode_MusicFull => Get("Mode.MusicFull");
    public static string Mode_CallsFull => Get("Mode.CallsFull");

    public static string Settings_Title => Get("Settings.Title");
    public static string Settings_General => Get("Settings.General");
    public static string Settings_AudioQuality => Get("Settings.AudioQuality");
    public static string Settings_AppRules => Get("Settings.AppRules");
    public static string Settings_Language => Get("Settings.Language");
    public static string Settings_BluetoothDevice => Get("Settings.BluetoothDevice");
    public static string Settings_Refresh => Get("Settings.Refresh");
    public static string Settings_DefaultMode => Get("Settings.DefaultMode");
    public static string Settings_MusicHighQuality => Get("Settings.MusicHighQuality");
    public static string Settings_CallsWithMic => Get("Settings.CallsWithMic");
    public static string Settings_Behavior => Get("Settings.Behavior");
    public static string Settings_AutoStart => Get("Settings.AutoStart");
    public static string Settings_ShowNotifications => Get("Settings.ShowNotifications");
    public static string Settings_AutoSwitchOnConnect => Get("Settings.AutoSwitchOnConnect");
    public static string Settings_AutoSwitchByApp => Get("Settings.AutoSwitchByApp");
    public static string Settings_ConfigureApps => Get("Settings.ConfigureApps");
    public static string Settings_Save => Get("Settings.Save");
    public static string Settings_Cancel => Get("Settings.Cancel");

    public static string Audio_CurrentStatus => Get("Audio.CurrentStatus");
    public static string Audio_RefreshInfo => Get("Audio.RefreshInfo");
    public static string Audio_PreferredCodec => Get("Audio.PreferredCodec");
    public static string Audio_CodecHint => Get("Audio.CodecHint");
    public static string Audio_Processing => Get("Audio.Processing");
    public static string Audio_DisableEnhancements => Get("Audio.DisableEnhancements");
    public static string Audio_DisableEnhancementsHint => Get("Audio.DisableEnhancementsHint");
    public static string Audio_SetAsDefault => Get("Audio.SetAsDefault");
    public static string Audio_Additional => Get("Audio.Additional");
    public static string Audio_ThreadPriority => Get("Audio.ThreadPriority");
    public static string Audio_ThreadPriorityHint => Get("Audio.ThreadPriorityHint");
    public static string Audio_ApplySettings => Get("Audio.ApplySettings");

    public static string Rules_Description => Get("Rules.Description");
    public static string Rules_Add => Get("Rules.Add");
    public static string Rules_Remove => Get("Rules.Remove");
    public static string Rules_MoveUp => Get("Rules.MoveUp");
    public static string Rules_MoveDown => Get("Rules.MoveDown");
    public static string Rules_Reset => Get("Rules.Reset");
    public static string Rules_Application => Get("Rules.Application");
    public static string Rules_Process => Get("Rules.Process");
    public static string Rules_Profile => Get("Rules.Profile");
    public static string Rules_Priority => Get("Rules.Priority");

    public static string Tray_Open => Get("Tray.Open");
    public static string Tray_Music => Get("Tray.Music");
    public static string Tray_Calls => Get("Tray.Calls");
    public static string Tray_Settings => Get("Tray.Settings");
    public static string Tray_Diagnostics => Get("Tray.Diagnostics");
    public static string Tray_Exit => Get("Tray.Exit");

    public static string Help_Title => Get("Help.Title");
    public static string Help_About => Get("Help.About");
    public static string Help_Description => Get("Help.Description");
    public static string Help_Author => Get("Help.Author");
    public static string Help_AuthorName => Get("Help.AuthorName");
    public static string Help_Donate => Get("Help.Donate");
    public static string Help_DonateDescription => Get("Help.DonateDescription");
    public static string Help_CopyBTC => Get("Help.CopyBTC");
    public static string Help_Copied => Get("Help.Copied");
    public static string Help_Version => Get("Help.Version");
    public static string Help_License => Get("Help.License");
    public static string Help_Close => Get("Help.Close");

    public static string Tab_Control => Get("Tab.Control");
    public static string Tab_Settings => Get("Tab.Settings");
    public static string Tab_Diagnostics => Get("Tab.Diagnostics");
    public static string Tab_About => Get("Tab.About");

    public static string Mode_MusicDesc => Get("Mode.MusicDesc");
    public static string Mode_CallsDesc => Get("Mode.CallsDesc");

    public static string About_Description => Get("About.Description");
    public static string About_Author => Get("About.Author");
    public static string About_AuthorName => Get("About.AuthorName");
    public static string About_Support => Get("About.Support");
    public static string About_SupportDesc => Get("About.SupportDesc");
    public static string About_Copy => Get("About.Copy");
    public static string About_License => Get("About.License");
    public static string About_VersionFormat => Get("About.VersionFormat");

    public static string Diag_Audio => Get("Diag.Audio");
    public static string Diag_Codec => Get("Diag.Codec");
    public static string Diag_OpenLogs => Get("Diag.OpenLogs");
    public static string Diag_Device => Get("Diag.Device");
    public static string Diag_AudioEndpoints => Get("Diag.AudioEndpoints");
    public static string Diag_A2dpEndpoint => Get("Diag.A2dpEndpoint");
    public static string Diag_HfpEndpoint => Get("Diag.HfpEndpoint");
    public static string Diag_Yes => Get("Diag.Yes");
    public static string Diag_No => Get("Diag.No");
    public static string Diag_NoConnectedDevices => Get("Diag.NoConnectedDevices");
    public static string Diag_PairedDevices => Get("Diag.PairedDevices");
    public static string Diag_PlaybackDevices => Get("Diag.PlaybackDevices");
    public static string Diag_BluetoothEndpoints => Get("Diag.BluetoothEndpoints");
    public static string Diag_EndpointsForDevice => Get("Diag.EndpointsForDevice");
    public static string Diag_CodecLabel => Get("Diag.CodecLabel");
    public static string Diag_Frequency => Get("Diag.Frequency");
    public static string Diag_BitDepth => Get("Diag.BitDepth");
    public static string Diag_Channels => Get("Diag.Channels");
    public static string Diag_Bitrate => Get("Diag.Bitrate");
    public static string Diag_NoActiveA2dp => Get("Diag.NoActiveA2dp");
    public static string Diag_CodecDataUnavailable => Get("Diag.CodecDataUnavailable");
    public static string Diag_Loading => Get("Diag.Loading");

    public static string Settings_Saved => Get("Settings.Saved");
    public static string Settings_SaveError => Get("Settings.SaveError");
    public static string Settings_Error => Get("Settings.Error");
    public static string Settings_AutoSwitchByAppHint => Get("Settings.AutoSwitchByAppHint");
    public static string Diag_LoadError => Get("Diag.LoadError");
    public static string Diag_RealCodec => Get("Diag.RealCodec");
    public static string Diag_EstimatedCodec => Get("Diag.EstimatedCodec");
    public static string Diag_WindowsCodecs => Get("Diag.WindowsCodecs");
    public static string Diag_AvailableCodecs => Get("Diag.AvailableCodecs");
    public static string Diag_Enabled => Get("Diag.Enabled");
    public static string Diag_Disabled => Get("Diag.Disabled");
    public static string Diag_AACEnabled => Get("Diag.AACEnabled");
    public static string Diag_AACDisabled => Get("Diag.AACDisabled");
    public static string Diag_ReconnectRequired => Get("Diag.ReconnectRequired");
    public static string Diag_AACToggleFailed => Get("Diag.AACToggleFailed");
    public static string Diag_DisableAACHint => Get("Diag.DisableAACHint");
    public static string Diag_IntelAdapterDetected => Get("Diag.IntelAdapterDetected");
    public static string Diag_IntelAACWarning => Get("Diag.IntelAACWarning");
    public static string Diag_DisableAAC => Get("Diag.DisableAAC");
    public static string Diag_EnableAAC => Get("Diag.EnableAAC");
    public static string Diag_AACStatus => Get("Diag.AACStatus");
    public static string Diag_BluetoothAdapter => Get("Diag.BluetoothAdapter");
    public static string Diag_ReconnectWarning => Get("Diag.ReconnectWarning");
    public static string Diag_Dismiss => Get("Diag.Dismiss");
    public static string Diag_RebootRequired => Get("Diag.RebootRequired");

    public static string Codec_TableHeader => Get("Codec.TableHeader");
    public static string Codec_ColumnCodec => Get("Codec.ColumnCodec");
    public static string Codec_ColumnBitrate => Get("Codec.ColumnBitrate");
    public static string Codec_ColumnQuality => Get("Codec.ColumnQuality");
    public static string Codec_ColumnNote => Get("Codec.ColumnNote");
    public static string Codec_Quality_Basic => Get("Codec.Quality.Basic");
    public static string Codec_Quality_Good => Get("Codec.Quality.Good");
    public static string Codec_Quality_Excellent => Get("Codec.Quality.Excellent");
    public static string Codec_Quality_High => Get("Codec.Quality.High");
    public static string Codec_Quality_HiRes => Get("Codec.Quality.HiRes");
    public static string Codec_Note_SBC => Get("Codec.Note.SBC");
    public static string Codec_Note_RequiresWin10 => Get("Codec.Note.RequiresWin10");
    public static string Codec_Note_NeedsAdapter => Get("Codec.Note.NeedsAdapter");
    public static string Codec_Note_ChangesAfterReconnect => Get("Codec.Note.ChangesAfterReconnect");
    public static string Codec_TableNote => Get("Codec.TableNote");
    public static string Codec_SupportedCodecs => Get("Codec.SupportedCodecs");

    public static string Codec_Auto => Get("Codec.Auto");
    public static string Codec_SBC => Get("Codec.SBC");
    public static string Codec_AAC => Get("Codec.AAC");
    public static string Codec_AptX => Get("Codec.AptX");
    public static string Codec_AptXHD => Get("Codec.AptXHD");
    public static string Codec_LDAC => Get("Codec.LDAC");
    public static string Codec_Unknown => Get("Codec.Unknown");

    public static string CodecDesc_Auto => Get("CodecDesc.Auto");
    public static string CodecDesc_SBC => Get("CodecDesc.SBC");
    public static string CodecDesc_AAC => Get("CodecDesc.AAC");
    public static string CodecDesc_AptX => Get("CodecDesc.AptX");
    public static string CodecDesc_AptXHD => Get("CodecDesc.AptXHD");
    public static string CodecDesc_LDAC => Get("CodecDesc.LDAC");

    public static string Status_CriticalError => Get("Status.CriticalError");
    public static string Status_InitError => Get("Status.InitError");
    public static string Status_DeviceNotConnected => Get("Status.DeviceNotConnected");
    public static string Status_Error => Get("Status.Error");
    public static string Status_SwitchError => Get("Status.SwitchError");
    public static string Status_Unknown => Get("Status.Unknown");

    public static string Notification_AdminRequired => Get("Notification.AdminRequired");
    public static string Notification_SwitchFailed => Get("Notification.SwitchFailed");
    public static string Notification_LogsFolderNotFound => Get("Notification.LogsFolderNotFound");

    public static string Dialog_Warning => Get("Dialog.Warning");
    public static string Dialog_Confirm => Get("Dialog.Confirm");

    public static string Audio_ClickRefresh => Get("Audio.ClickRefresh");
    public static string Audio_AptxHdAvailable => Get("Audio.AptxHdAvailable");

    public static string Priority_Normal => Get("Priority.Normal");
    public static string Priority_High => Get("Priority.High");
    public static string Priority_Critical => Get("Priority.Critical");

    public static string Diag_A2dpSupported => Get("Diag.A2dpSupported");
    public static string Diag_HfpSupported => Get("Diag.HfpSupported");
    public static string Diag_AvrcpSupported => Get("Diag.AvrcpSupported");
    public static string Diag_EnhancementsEnabled => Get("Diag.EnhancementsEnabled");
    public static string Diag_EnhancementsDisabled => Get("Diag.EnhancementsDisabled");
    public static string Diag_CodecInfoUnavailable => Get("Diag.CodecInfoUnavailable");
    public static string Diag_ServiceNotInit => Get("Diag.ServiceNotInit");
    public static string Diag_AutoStart => Get("Diag.AutoStart");
    public static string Diag_AutoSwitch => Get("Diag.AutoSwitch");
    public static string Diag_Notifications => Get("Diag.Notifications");
    public static string Diag_ErrorGettingData => Get("Diag.ErrorGettingData");
    public static string Diag_NoLogsFound => Get("Diag.NoLogsFound");

    public static string App_Application => Get("App.Application");

    private static void InitializeStrings()
    {
        Add("AppName", "A2DP Commander", "A2DP Commander");
        Add("AppDescription", "Управление Bluetooth аудио профилями", "Bluetooth Audio Profile Manager");

        Add("MainWindow.Title", "A2DP Commander", "A2DP Commander");
        Add("MainWindow.Subtitle", "Управление Bluetooth аудио профилями", "Bluetooth Audio Profile Manager");
        Add("MainWindow.CurrentMode", "Текущий режим:", "Current mode:");
        Add("MainWindow.Music", "Музыка", "Music");
        Add("MainWindow.Calls", "Звонки", "Calls");
        Add("MainWindow.MusicDescription", "A2DP — высокое качество", "A2DP — high quality");
        Add("MainWindow.CallsDescription", "HFP — с микрофоном", "HFP — with microphone");
        Add("MainWindow.Settings", "Настройки", "Settings");
        Add("MainWindow.Diagnostics", "Диагностика", "Diagnostics");
        Add("MainWindow.MinimizeToTray", "Свернуть в трей", "Minimize to tray");
        Add("MainWindow.Help", "Справка", "Help");

        Add("Device.NotConnected", "Устройство не подключено", "Device not connected");
        Add("Device.Connected", "Подключено", "Connected");

        Add("Mode.Music", "Музыка", "Music");
        Add("Mode.Calls", "Звонки", "Calls");
        Add("Mode.Unknown", "Не определён", "Unknown");
        Add("Mode.MusicFull", "Музыка (A2DP)", "Music (A2DP)");
        Add("Mode.CallsFull", "Звонки (HFP)", "Calls (HFP)");

        Add("Settings.Title", "A2DP Commander — Настройки", "A2DP Commander — Settings");
        Add("Settings.General", "Основные", "General");
        Add("Settings.AudioQuality", "Качество звука", "Audio Quality");
        Add("Settings.AppRules", "Правила приложений", "App Rules");
        Add("Settings.Language", "Язык / Language", "Language");
        Add("Settings.BluetoothDevice", "Bluetooth устройство", "Bluetooth Device");
        Add("Settings.Refresh", "Обновить", "Refresh");
        Add("Settings.DefaultMode", "Режим по умолчанию", "Default Mode");
        Add("Settings.MusicHighQuality", "Музыка (A2DP) — высокое качество звука", "Music (A2DP) — high quality audio");
        Add("Settings.CallsWithMic", "Звонки (HFP) — с микрофоном", "Calls (HFP) — with microphone");
        Add("Settings.Behavior", "Поведение", "Behavior");
        Add("Settings.AutoStart", "Запускать при старте Windows", "Start with Windows");
        Add("Settings.ShowNotifications", "Показывать уведомления", "Show notifications");
        Add("Settings.AutoSwitchOnConnect", "Автоматически переключать режим при подключении", "Auto-switch mode on connect");
        Add("Settings.AutoSwitchByApp", "Автоматически переключать профиль по приложениям", "Auto-switch profile by application");
        Add("Settings.ConfigureApps", "Настроить приложения...", "Configure apps...");
        Add("Settings.Save", "Сохранить", "Save");
        Add("Settings.Cancel", "Отмена", "Cancel");

        Add("Audio.CurrentStatus", "Текущий статус", "Current Status");
        Add("Audio.RefreshInfo", "Обновить информацию", "Refresh Info");
        Add("Audio.PreferredCodec", "Предпочитаемый кодек", "Preferred Codec");
        Add("Audio.CodecHint", "Выберите кодек для Bluetooth аудио (требует переподключения):", "Select codec for Bluetooth audio (requires reconnect):");
        Add("Audio.Processing", "Обработка звука", "Audio Processing");
        Add("Audio.DisableEnhancements", "Отключить улучшения звука Windows", "Disable Windows audio enhancements");
        Add("Audio.DisableEnhancementsHint", "Рекомендуется отключить для лучшего качества", "Recommended to disable for better quality");
        Add("Audio.SetAsDefault", "Направлять весь звук на BT наушники при подключении", "Route all audio to BT headphones when connected");
        Add("Audio.Additional", "Дополнительно", "Additional");
        Add("Audio.ThreadPriority", "Приоритет аудио потока:", "Audio thread priority:");
        Add("Audio.ThreadPriorityHint", "Высокий приоритет уменьшает прерывания звука", "High priority reduces audio interruptions");
        Add("Audio.ApplySettings", "Применить настройки качества", "Apply quality settings");

        Add("Rules.Description", "Настройте автоматическое переключение профиля при запуске приложений. Правило с наивысшим приоритетом побеждает при конфликте.",
            "Configure automatic profile switching when applications start. Higher priority rule wins on conflict.");
        Add("Rules.Add", "Добавить", "Add");
        Add("Rules.Remove", "Удалить", "Remove");
        Add("Rules.MoveUp", "Вверх", "Up");
        Add("Rules.MoveDown", "Вниз", "Down");
        Add("Rules.Reset", "Сбросить", "Reset");
        Add("Rules.Application", "Приложение", "Application");
        Add("Rules.Process", "Процесс", "Process");
        Add("Rules.Profile", "Профиль", "Profile");
        Add("Rules.Priority", "Приоритет", "Priority");

        Add("Tray.Open", "Открыть A2DP Commander", "Open A2DP Commander");
        Add("Tray.Music", "Музыка (A2DP)", "Music (A2DP)");
        Add("Tray.Calls", "Звонки (HFP)", "Calls (HFP)");
        Add("Tray.Settings", "Настройки...", "Settings...");
        Add("Tray.Diagnostics", "Диагностика...", "Diagnostics...");
        Add("Tray.Exit", "Выход", "Exit");

        Add("Help.Title", "О программе — A2DP Commander", "About — A2DP Commander");
        Add("Help.About", "О программе", "About");
        Add("Help.Description",
            "A2DP Commander — бесплатная утилита для управления Bluetooth аудио профилями (A2DP/HFP) в Windows.\n\nПозволяет быстро переключаться между режимами высокого качества звука и режимом с микрофоном для любых Bluetooth наушников.",
            "A2DP Commander — a free utility for managing Bluetooth audio profiles (A2DP/HFP) in Windows.\n\nAllows you to quickly switch between high-quality audio mode and microphone mode for any Bluetooth headphones.");
        Add("Help.Author", "Автор:", "Author:");
        Add("Help.AuthorName", "Андрей Юмашев / Andrey Yumashev", "Andrey Yumashev");
        Add("Help.Donate", "Поддержать проект", "Support the Project");
        Add("Help.DonateDescription", "Если программа оказалась полезной, вы можете поддержать разработку:", "If you find this program useful, you can support development:");
        Add("Help.CopyBTC", "Копировать BTC адрес", "Copy BTC address");
        Add("Help.Copied", "Скопировано!", "Copied!");
        Add("Help.Version", "Версия:", "Version:");
        Add("Help.License", "Лицензия: MIT License", "License: MIT License");
        Add("Help.Close", "Закрыть", "Close");

        Add("Tab.Control", "Управление", "Control");
        Add("Tab.Settings", "Настройки", "Settings");
        Add("Tab.Diagnostics", "Диагностика", "Diagnostics");
        Add("Tab.About", "О программе", "About");

        Add("Mode.MusicDesc", "A2DP — высокое качество", "A2DP — high quality");
        Add("Mode.CallsDesc", "HFP — с микрофоном", "HFP — with microphone");

        Add("About.Description",
            "A2DP Commander — бесплатная утилита для управления Bluetooth аудио профилями (A2DP/HFP) в Windows.\n\nПозволяет быстро переключаться между режимами высокого качества звука и режимом с микрофоном для любых Bluetooth наушников.",
            "A2DP Commander — a free utility for managing Bluetooth audio profiles (A2DP/HFP) in Windows.\n\nAllows you to quickly switch between high-quality audio mode and microphone mode for any Bluetooth headphones.");
        Add("About.Author", "Автор:", "Author:");
        Add("About.AuthorName", "Андрей Юмашев", "Andrey Yumashev");
        Add("About.Support", "Поддержать проект", "Support the Project");
        Add("About.SupportDesc", "Если программа оказалась полезной, вы можете поддержать разработку:", "If you find this program useful, you can support development:");
        Add("About.Copy", "Копировать", "Copy");
        Add("About.License", "Лицензия: MIT License", "License: MIT License");
        Add("About.VersionFormat", "Версия {0}", "Version {0}");

        Add("Diag.Audio", "Аудио устройства", "Audio Devices");
        Add("Diag.Codec", "Кодек", "Codec");
        Add("Diag.OpenLogs", "Открыть логи", "Open Logs");
        Add("Diag.Device", "Устройство", "Device");
        Add("Diag.AudioEndpoints", "Audio endpoints", "Audio endpoints");
        Add("Diag.A2dpEndpoint", "A2DP endpoint", "A2DP endpoint");
        Add("Diag.HfpEndpoint", "HFP endpoint", "HFP endpoint");
        Add("Diag.Yes", "Да", "Yes");
        Add("Diag.No", "Нет", "No");
        Add("Diag.NoConnectedDevices", "Нет подключённых устройств", "No connected devices");
        Add("Diag.PairedDevices", "Сопряжённых", "Paired");
        Add("Diag.PlaybackDevices", "Устройств воспроизведения", "Playback devices");
        Add("Diag.BluetoothEndpoints", "Bluetooth endpoints", "Bluetooth endpoints");
        Add("Diag.EndpointsForDevice", "Endpoints для устройства", "Endpoints for device");
        Add("Diag.CodecLabel", "Кодек", "Codec");
        Add("Diag.Frequency", "Частота", "Frequency");
        Add("Diag.BitDepth", "Глубина", "Bit depth");
        Add("Diag.Channels", "Каналы", "Channels");
        Add("Diag.Bitrate", "Битрейт", "Bitrate");
        Add("Diag.NoActiveA2dp", "Нет активного A2DP подключения", "No active A2DP connection");
        Add("Diag.CodecDataUnavailable", "Подключение есть, данные кодека недоступны", "Connected, but codec data unavailable");
        Add("Diag.Loading", "Загрузка...", "Loading...");

        Add("Settings.Saved", "Настройки сохранены", "Settings saved");
        Add("Settings.SaveError", "Не удалось сохранить настройки", "Failed to save settings");
        Add("Settings.Error", "Ошибка", "Error");
        Add("Settings.AutoSwitchByAppHint", "Автоматически переключает профиль при запуске определённых приложений (Discord, Zoom → Звонки)", "Automatically switches profile when specific apps start (Discord, Zoom → Calls)");
        Add("Diag.LoadError", "Ошибка загрузки", "Loading error");
        Add("Diag.RealCodec", "реальный", "real");
        Add("Diag.EstimatedCodec", "оценка", "estimated");
        Add("Diag.WindowsCodecs", "Настройки кодеков Windows", "Windows Codec Settings");
        Add("Diag.AvailableCodecs", "Доступные кодеки", "Available codecs");
        Add("Diag.Enabled", "Включён", "Enabled");
        Add("Diag.Disabled", "Отключён", "Disabled");
        Add("Diag.AACEnabled", "AAC кодек включён", "AAC codec enabled");
        Add("Diag.AACDisabled", "AAC кодек отключён. Windows будет использовать SBC.", "AAC codec disabled. Windows will use SBC.");
        Add("Diag.ReconnectRequired", "Переподключите Bluetooth устройство для применения изменений.", "Reconnect Bluetooth device for changes to take effect.");
        Add("Diag.AACToggleFailed", "Не удалось изменить настройку AAC. Требуются права администратора.", "Failed to change AAC setting. Administrator rights required.");
        Add("Diag.DisableAACHint", "Отключение AAC может решить проблему запинаний на Intel адаптерах", "Disabling AAC may fix stuttering issues on Intel adapters");
        Add("Diag.IntelAdapterDetected", "Обнаружен Intel Bluetooth адаптер", "Intel Bluetooth adapter detected");
        Add("Diag.IntelAACWarning", "Intel адаптеры часто имеют проблемы с AAC кодеком, что вызывает запинания звука. Рекомендуется отключить AAC.", "Intel adapters often have issues with AAC codec causing audio stuttering. Disabling AAC is recommended.");
        Add("Diag.DisableAAC", "Отключить AAC", "Disable AAC");
        Add("Diag.EnableAAC", "Включить AAC", "Enable AAC");
        Add("Diag.AACStatus", "Статус AAC:", "AAC Status:");
        Add("Diag.BluetoothAdapter", "Bluetooth адаптер:", "Bluetooth adapter:");
        Add("Diag.ReconnectWarning", "Настройки AAC изменены! Для применения требуется перезагрузка компьютера.", "AAC settings changed! A computer restart is required to apply changes.");
        Add("Diag.Dismiss", "Понятно", "Dismiss");
        Add("Diag.RebootRequired", "Перезагрузить", "Reboot");

        Add("Codec.TableHeader", "Bluetooth кодеки", "Bluetooth Codecs");
        Add("Codec.ColumnCodec", "Кодек", "Codec");
        Add("Codec.ColumnBitrate", "Битрейт", "Bitrate");
        Add("Codec.ColumnQuality", "Качество", "Quality");
        Add("Codec.ColumnNote", "Примечание", "Note");
        Add("Codec.Quality.Basic", "Базовое", "Basic");
        Add("Codec.Quality.Good", "Хорошее", "Good");
        Add("Codec.Quality.Excellent", "Отличное", "Excellent");
        Add("Codec.Quality.High", "Высокое", "High");
        Add("Codec.Quality.HiRes", "Hi-Res", "Hi-Res");
        Add("Codec.Note.SBC", "Стандартный, всегда доступен", "Standard, always available");
        Add("Codec.Note.RequiresWin10", "Требуется Win 10 2004+", "Requires Win 10 2004+");
        Add("Codec.Note.NeedsAdapter", "Нужен специальный BT адаптер", "Requires special BT adapter");
        Add("Codec.Note.ChangesAfterReconnect", "Изменения применятся после переподключения", "Changes take effect after reconnection");
        Add("Codec.TableNote", "Примечание: Windows поддерживает SBC по умолчанию. AAC требует Windows 10 2004+. aptX/LDAC требуют специальный Bluetooth адаптер (Creative BT-W5, FiiO BTA30 Pro и др.).",
            "Note: Windows supports SBC by default. AAC requires Windows 10 2004+. aptX/LDAC require special Bluetooth adapter (Creative BT-W5, FiiO BTA30 Pro, etc.).");
        Add("Codec.SupportedCodecs", "Поддерживаемые кодеки:", "Supported codecs:");

        Add("Codec.Auto", "Автоматически", "Auto");
        Add("Codec.SBC", "SBC (базовый)", "SBC (basic)");
        Add("Codec.AAC", "AAC (хороший)", "AAC (good)");
        Add("Codec.AptX", "aptX (отличный)", "aptX (excellent)");
        Add("Codec.AptXHD", "aptX HD (высший)", "aptX HD (high-end)");
        Add("Codec.LDAC", "LDAC (максимум)", "LDAC (maximum)");
        Add("Codec.Unknown", "Неизвестно", "Unknown");

        Add("CodecDesc.Auto", "Система выберет лучший доступный кодек автоматически.", "System will select the best available codec automatically.");
        Add("CodecDesc.SBC", "Базовый кодек, 328 kbps. Поддерживается всеми устройствами.", "Basic codec, 328 kbps. Supported by all devices.");
        Add("CodecDesc.AAC", "Хорошее качество, 256 kbps. Популярен на устройствах Apple.", "Good quality, 256 kbps. Popular on Apple devices.");
        Add("CodecDesc.AptX", "Отличное качество, 352 kbps. Требует поддержки Qualcomm.", "Excellent quality, 352 kbps. Requires Qualcomm support.");
        Add("CodecDesc.AptXHD", "Высшее качество, 576 kbps. Требует aptX HD поддержки.", "High-end quality, 576 kbps. Requires aptX HD support.");
        Add("CodecDesc.LDAC", "Максимальное качество, до 990 kbps. Требует специальный драйвер.", "Maximum quality, up to 990 kbps. Requires special driver.");

        Add("Status.CriticalError", "Критическая ошибка инициализации", "Critical initialization error");
        Add("Status.InitError", "Ошибка инициализации", "Initialization error");
        Add("Status.DeviceNotConnected", "Устройство не подключено", "Device not connected");
        Add("Status.Error", "Ошибка", "Error");
        Add("Status.SwitchError", "Ошибка переключения", "Switch error");
        Add("Status.Unknown", "Неизвестно", "Unknown");

        Add("Notification.AdminRequired", "Требуются права администратора для смены режима", "Administrator rights required to change mode");
        Add("Notification.SwitchFailed", "Не удалось переключить режим", "Failed to switch mode");
        Add("Notification.LogsFolderNotFound", "Папка с логами не найдена", "Logs folder not found");

        Add("Dialog.Warning", "Внимание", "Warning");
        Add("Dialog.Confirm", "Подтверждение", "Confirmation");

        Add("Audio.ClickRefresh", "Нажмите 'Обновить' для получения информации", "Click 'Refresh' to get information");
        Add("Audio.AptxHdAvailable", "Доступен", "Available");

        Add("Priority.Normal", "Обычный", "Normal");
        Add("Priority.High", "Высокий", "High");
        Add("Priority.Critical", "Критический", "Critical");

        Add("Diag.A2dpSupported", "A2DP", "A2DP");
        Add("Diag.HfpSupported", "HFP", "HFP");
        Add("Diag.AvrcpSupported", "AVRCP", "AVRCP");
        Add("Diag.EnhancementsEnabled", "Улучшения Windows: Включены", "Windows Enhancements: Enabled");
        Add("Diag.EnhancementsDisabled", "Улучшения Windows: Отключены", "Windows Enhancements: Disabled");
        Add("Diag.CodecInfoUnavailable", "Информация о кодеке недоступна", "Codec information unavailable");
        Add("Diag.ServiceNotInit", "Сервис качества звука не инициализирован", "Audio quality service not initialized");
        Add("Diag.AutoStart", "Автозапуск", "Autostart");
        Add("Diag.AutoSwitch", "Автопереключение", "Auto-switch");
        Add("Diag.Notifications", "Уведомления", "Notifications");
        Add("Diag.ErrorGettingData", "Ошибка получения данных", "Error getting data");
        Add("Diag.NoLogsFound", "Логи не найдены", "No logs found");

        Add("App.Application", "приложение", "application");
    }

    private static void Add(string key, string russian, string english)
    {
        _strings[key] = new Dictionary<Language, string>
        {
            { Language.Russian, russian },
            { Language.English, english }
        };
    }
}

public enum Language
{
    Russian,
    English
}
