using System.Runtime.InteropServices;
using System.Windows.Interop;
using BTAudioDriver.Models;
using Serilog;

namespace BTAudioDriver.Services;

public class HotkeyService : IHotkeyService
{
    private static readonly ILogger Logger = Log.ForContext<HotkeyService>();

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_TOGGLE = 1;
    private const int HOTKEY_MUSIC = 2;
    private const int HOTKEY_CALLS = 3;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _isRegistered;
    private bool _disposed;

    public event EventHandler? ToggleModeRequested;
    public event EventHandler? MusicModeRequested;
    public event EventHandler? CallsModeRequested;

    public bool Register(HotkeyConfig? toggleHotkey)
    {
        if (_isRegistered)
        {
            Unregister();
        }

        if (toggleHotkey?.IsEnabled != true)
        {
            Logger.Information("Hotkeys are disabled");
            return true;
        }

        var helper = new WindowInteropHelper(System.Windows.Application.Current.MainWindow
            ?? new System.Windows.Window { ShowInTaskbar = false, WindowStyle = System.Windows.WindowStyle.None });

        if (helper.Handle == IntPtr.Zero)
        {
            helper.EnsureHandle();
        }

        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero)
        {
            _hwndSource = new HwndSource(new HwndSourceParameters("BTAudioDriverHotkey")
            {
                Width = 0,
                Height = 0,
                PositionX = -100,
                PositionY = -100,
                WindowStyle = 0,
                ParentWindow = IntPtr.Zero
            });
            _hwnd = _hwndSource.Handle;
        }
        else
        {
            _hwndSource = HwndSource.FromHwnd(_hwnd);
        }

        _hwndSource?.AddHook(WndProc);

        var mods = ConvertModifiers(toggleHotkey.Modifiers) | MOD_NOREPEAT;
        var success = RegisterHotKey(_hwnd, HOTKEY_TOGGLE, mods, (uint)toggleHotkey.VirtualKeyCode);

        if (success)
        {
            Logger.Information("Registered toggle hotkey: {Hotkey}", toggleHotkey);
        }
        else
        {
            Logger.Warning("Failed to register toggle hotkey: {Hotkey}", toggleHotkey);
        }

        _isRegistered = success;
        return success;
    }

    public void Unregister()
    {
        if (!_isRegistered || _hwnd == IntPtr.Zero)
            return;

        UnregisterHotKey(_hwnd, HOTKEY_TOGGLE);

        _hwndSource?.RemoveHook(WndProc);
        _isRegistered = false;

        Logger.Information("Unregistered hotkey");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var hotkeyId = wParam.ToInt32();

            switch (hotkeyId)
            {
                case HOTKEY_TOGGLE:
                    Logger.Debug("Toggle hotkey pressed");
                    ToggleModeRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;

                case HOTKEY_MUSIC:
                    Logger.Debug("Music mode hotkey pressed");
                    MusicModeRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;

                case HOTKEY_CALLS:
                    Logger.Debug("Calls mode hotkey pressed");
                    CallsModeRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint result = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Win)) result |= MOD_WIN;

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Unregister();

        if (_hwndSource != null)
        {
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
