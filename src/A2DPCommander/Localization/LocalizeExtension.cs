using System.Windows.Markup;

namespace BTAudioDriver.Localization;

[MarkupExtensionReturnType(typeof(string))]
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new System.Windows.Data.Binding(Key)
        {
            Source = LocalizationManager.Instance,
            Mode = System.Windows.Data.BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}

public class LocalizationManager : System.ComponentModel.INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager()
    {
        Strings.LanguageChanged += (_, _) => RefreshAll();
    }

    private void RefreshAll()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(null));
    }

    public string AppName => Strings.AppName;
    public string AppDescription => Strings.AppDescription;

    public string MainWindow_Title => Strings.MainWindow_Title;
    public string MainWindow_Subtitle => Strings.MainWindow_Subtitle;
    public string MainWindow_CurrentMode => Strings.MainWindow_CurrentMode;
    public string MainWindow_Music => Strings.MainWindow_Music;
    public string MainWindow_Calls => Strings.MainWindow_Calls;
    public string MainWindow_MusicDescription => Strings.MainWindow_MusicDescription;
    public string MainWindow_CallsDescription => Strings.MainWindow_CallsDescription;
    public string MainWindow_Settings => Strings.MainWindow_Settings;
    public string MainWindow_Diagnostics => Strings.MainWindow_Diagnostics;
    public string MainWindow_MinimizeToTray => Strings.MainWindow_MinimizeToTray;
    public string MainWindow_Help => Strings.MainWindow_Help;

    public string Device_NotConnected => Strings.Device_NotConnected;
    public string Device_Connected => Strings.Device_Connected;

    public string Mode_Music => Strings.Mode_Music;
    public string Mode_Calls => Strings.Mode_Calls;
    public string Mode_Unknown => Strings.Mode_Unknown;
    public string Mode_MusicFull => Strings.Mode_MusicFull;
    public string Mode_CallsFull => Strings.Mode_CallsFull;

    public string Settings_Title => Strings.Settings_Title;
    public string Settings_General => Strings.Settings_General;
    public string Settings_AudioQuality => Strings.Settings_AudioQuality;
    public string Settings_AppRules => Strings.Settings_AppRules;
    public string Settings_Language => Strings.Settings_Language;
    public string Settings_BluetoothDevice => Strings.Settings_BluetoothDevice;
    public string Settings_Refresh => Strings.Settings_Refresh;
    public string Settings_DefaultMode => Strings.Settings_DefaultMode;
    public string Settings_MusicHighQuality => Strings.Settings_MusicHighQuality;
    public string Settings_CallsWithMic => Strings.Settings_CallsWithMic;
    public string Settings_Behavior => Strings.Settings_Behavior;
    public string Settings_AutoStart => Strings.Settings_AutoStart;
    public string Settings_ShowNotifications => Strings.Settings_ShowNotifications;
    public string Settings_AutoSwitchOnConnect => Strings.Settings_AutoSwitchOnConnect;
    public string Settings_AutoSwitchByApp => Strings.Settings_AutoSwitchByApp;
    public string Settings_ConfigureApps => Strings.Settings_ConfigureApps;
    public string Settings_Save => Strings.Settings_Save;
    public string Settings_Cancel => Strings.Settings_Cancel;

    public string Tray_Open => Strings.Tray_Open;
    public string Tray_Music => Strings.Tray_Music;
    public string Tray_Calls => Strings.Tray_Calls;
    public string Tray_Settings => Strings.Tray_Settings;
    public string Tray_Diagnostics => Strings.Tray_Diagnostics;
    public string Tray_Exit => Strings.Tray_Exit;

    public string Help_Title => Strings.Help_Title;
    public string Help_About => Strings.Help_About;
    public string Help_Description => Strings.Help_Description;
    public string Help_Author => Strings.Help_Author;
    public string Help_AuthorName => Strings.Help_AuthorName;
    public string Help_Donate => Strings.Help_Donate;
    public string Help_DonateDescription => Strings.Help_DonateDescription;
    public string Help_CopyBTC => Strings.Help_CopyBTC;
    public string Help_Copied => Strings.Help_Copied;
    public string Help_Version => Strings.Help_Version;
    public string Help_License => Strings.Help_License;
    public string Help_Close => Strings.Help_Close;

    public string Tab_Control => Strings.Tab_Control;
    public string Tab_Settings => Strings.Tab_Settings;
    public string Tab_Diagnostics => Strings.Tab_Diagnostics;
    public string Tab_About => Strings.Tab_About;

    public string Mode_MusicDesc => Strings.Mode_MusicDesc;
    public string Mode_CallsDesc => Strings.Mode_CallsDesc;

    public string About_Description => Strings.About_Description;
    public string About_Author => Strings.About_Author;
    public string About_AuthorName => Strings.About_AuthorName;
    public string About_Support => Strings.About_Support;
    public string About_SupportDesc => Strings.About_SupportDesc;
    public string About_Copy => Strings.About_Copy;
    public string About_License => Strings.About_License;
    public string About_VersionFormat => Strings.About_VersionFormat;

    public string Diag_Audio => Strings.Diag_Audio;
    public string Diag_Codec => Strings.Diag_Codec;
    public string Diag_OpenLogs => Strings.Diag_OpenLogs;

    public string Settings_AutoSwitchByAppHint => Strings.Settings_AutoSwitchByAppHint;

    public string Diag_RealCodec => Strings.Diag_RealCodec;
    public string Diag_EstimatedCodec => Strings.Diag_EstimatedCodec;

    public string Diag_IntelAdapterDetected => Strings.Diag_IntelAdapterDetected;
    public string Diag_IntelAACWarning => Strings.Diag_IntelAACWarning;
    public string Diag_DisableAAC => Strings.Diag_DisableAAC;
    public string Diag_EnableAAC => Strings.Diag_EnableAAC;
    public string Diag_AACStatus => Strings.Diag_AACStatus;
    public string Diag_DisableAACHint => Strings.Diag_DisableAACHint;
    public string Diag_ReconnectWarning => Strings.Diag_ReconnectWarning;
    public string Diag_Dismiss => Strings.Diag_Dismiss;
    public string Diag_RebootRequired => Strings.Diag_RebootRequired;

    public string Audio_CurrentStatus => Strings.Audio_CurrentStatus;
    public string Audio_RefreshInfo => Strings.Audio_RefreshInfo;
    public string Audio_PreferredCodec => Strings.Audio_PreferredCodec;
    public string Audio_CodecHint => Strings.Audio_CodecHint;
    public string Audio_Processing => Strings.Audio_Processing;
    public string Audio_DisableEnhancements => Strings.Audio_DisableEnhancements;
    public string Audio_DisableEnhancementsHint => Strings.Audio_DisableEnhancementsHint;
    public string Audio_SetAsDefault => Strings.Audio_SetAsDefault;
    public string Audio_Additional => Strings.Audio_Additional;
    public string Audio_OptimizeMMCSS => Strings.Audio_OptimizeMMCSS;
    public string Audio_OptimizeMMCSSHint => Strings.Audio_OptimizeMMCSSHint;
    public string Audio_ApplySettings => Strings.Audio_ApplySettings;

    public string Rules_Description => Strings.Rules_Description;
    public string Rules_Add => Strings.Rules_Add;
    public string Rules_Remove => Strings.Rules_Remove;
    public string Rules_MoveUp => Strings.Rules_MoveUp;
    public string Rules_MoveDown => Strings.Rules_MoveDown;
    public string Rules_Reset => Strings.Rules_Reset;
    public string Rules_Application => Strings.Rules_Application;
    public string Rules_Process => Strings.Rules_Process;
    public string Rules_Profile => Strings.Rules_Profile;
    public string Rules_Priority => Strings.Rules_Priority;

    public string Codec_TableHeader => Strings.Codec_TableHeader;
    public string Codec_ColumnCodec => Strings.Codec_ColumnCodec;
    public string Codec_ColumnBitrate => Strings.Codec_ColumnBitrate;
    public string Codec_ColumnQuality => Strings.Codec_ColumnQuality;
    public string Codec_ColumnNote => Strings.Codec_ColumnNote;
    public string Codec_Quality_Basic => Strings.Codec_Quality_Basic;
    public string Codec_Quality_Good => Strings.Codec_Quality_Good;
    public string Codec_Quality_Excellent => Strings.Codec_Quality_Excellent;
    public string Codec_Quality_High => Strings.Codec_Quality_High;
    public string Codec_Quality_HiRes => Strings.Codec_Quality_HiRes;
    public string Codec_Note_SBC => Strings.Codec_Note_SBC;
    public string Codec_Note_RequiresWin10 => Strings.Codec_Note_RequiresWin10;
    public string Codec_Note_NeedsAdapter => Strings.Codec_Note_NeedsAdapter;
    public string Codec_Note_ChangesAfterReconnect => Strings.Codec_Note_ChangesAfterReconnect;
    public string Codec_TableNote => Strings.Codec_TableNote;
    public string Codec_SupportedCodecs => Strings.Codec_SupportedCodecs;

    public string Adapter_Title => Strings.Adapter_Title;
    public string Adapter_Select => Strings.Adapter_Select;
    public string Adapter_Current => Strings.Adapter_Current;
    public string Adapter_SupportedCodecs => Strings.Adapter_SupportedCodecs;
    public string Adapter_Switch => Strings.Adapter_Switch;
    public string Adapter_NoAdapters => Strings.Adapter_NoAdapters;
    public string Adapter_Active => Strings.Adapter_Active;
    public string Adapter_Disabled => Strings.Adapter_Disabled;
    public string Adapter_Warning => Strings.Adapter_Warning;
    public string Adapter_SwitchWarning => Strings.Adapter_SwitchWarning;
    public string Adapter_SwitchSuccess => Strings.Adapter_SwitchSuccess;
    public string Adapter_SwitchFailed => Strings.Adapter_SwitchFailed;
    public string Adapter_Refresh => Strings.Adapter_Refresh;
}
