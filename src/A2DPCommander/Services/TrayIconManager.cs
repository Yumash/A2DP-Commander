using System.Drawing;
using System.Windows.Forms;
using BTAudioDriver.Localization;
using BTAudioDriver.Models;
using Serilog;

namespace BTAudioDriver.Services;

public class TrayIconManager : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<TrayIconManager>();

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _openMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    private readonly Icon _musicIcon;
    private readonly Icon _callsIcon;
    private readonly Icon _disconnectedIcon;

    private bool _disposed;

    public event EventHandler? ExitRequested;
    public event EventHandler? ShowMainWindowRequested;

    public TrayIconManager()
    {
        _musicIcon = CreateSimpleIcon(Color.FromArgb(0, 120, 215));
        _callsIcon = CreateSimpleIcon(Color.FromArgb(16, 124, 16));
        _disconnectedIcon = CreateSimpleIcon(Color.Gray);

        _contextMenu = new ContextMenuStrip();

        _openMenuItem = new ToolStripMenuItem(Strings.Tray_Open)
        {
            Font = new Font(_contextMenu.Font, FontStyle.Bold)
        };
        _openMenuItem.Click += (_, _) => ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);

        _exitMenuItem = new ToolStripMenuItem(Strings.Tray_Exit);
        _exitMenuItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _contextMenu.Items.Add(_openMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _disconnectedIcon,
            Text = "A2DP Commander",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (_, _) =>
        {
            ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
        };

        Logger.Information("Tray icon initialized");
    }

    public void UpdateState(DeviceProfileState? state)
    {
        if (state == null)
        {
            _notifyIcon.Icon = _disconnectedIcon;
            _notifyIcon.Text = $"{Strings.AppName} - {Strings.Device_NotConnected}";
            return;
        }

        switch (state.CurrentMode)
        {
            case ProfileMode.Music:
                _notifyIcon.Icon = _musicIcon;
                _notifyIcon.Text = $"{Strings.AppName} - {state.DeviceName} ({Strings.Mode_Music})";
                break;

            case ProfileMode.Calls:
                _notifyIcon.Icon = _callsIcon;
                _notifyIcon.Text = $"{Strings.AppName} - {state.DeviceName} ({Strings.Mode_Calls})";
                break;

            default:
                _notifyIcon.Icon = _disconnectedIcon;
                _notifyIcon.Text = $"{Strings.AppName} - {state.DeviceName}";
                break;
        }
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    public void ShowModeChangedNotification(ProfileMode newMode)
    {
        var modeText = newMode switch
        {
            ProfileMode.Music => Strings.Mode_MusicFull,
            ProfileMode.Calls => Strings.Mode_CallsFull,
            _ => Strings.Mode_Unknown
        };

        ShowNotification(Strings.AppName, modeText);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _musicIcon.Dispose();
        _callsIcon.Dispose();
        _disconnectedIcon.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static Icon CreateSimpleIcon(Color color)
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        var hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon).Clone() as Icon;
        DestroyIcon(hIcon);
        return icon!;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
