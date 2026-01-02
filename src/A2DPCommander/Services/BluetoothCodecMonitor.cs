using System.Security.Principal;
using BTAudioDriver.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Serilog;

namespace BTAudioDriver.Services;

public class BluetoothA2DPCodecInfo
{
    public byte StandardCodecId { get; set; }
    public int VendorId { get; set; }
    public int VendorCodecId { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.Now;

    public BluetoothCodec ToBluetoothCodec()
    {
        if (StandardCodecId != 0xFF)
        {
            return StandardCodecId switch
            {
                0x00 => BluetoothCodec.SBC,
                0x02 => BluetoothCodec.AAC,
                _ => BluetoothCodec.SBC
            };
        }

        return (VendorId, VendorCodecId) switch
        {
            (0x004F, 0x01) => BluetoothCodec.AptX,
            (0x00D7, 0x24) => BluetoothCodec.AptXHD,
            (0x004F, 0x02) => BluetoothCodec.AptXLL,
            (0x00D7, 0xAD) => BluetoothCodec.AptXAdaptive,

            (0x012D, 0xAA) => BluetoothCodec.LDAC,

            _ => BluetoothCodec.SBC
        };
    }

    public string GetCodecName()
    {
        if (StandardCodecId != 0xFF)
        {
            return StandardCodecId switch
            {
                0x00 => "SBC",
                0x01 => "MP3",
                0x02 => "AAC",
                0x04 => "ATRAC",
                _ => $"Unknown (0x{StandardCodecId:X2})"
            };
        }

        return (VendorId, VendorCodecId) switch
        {
            (0x004F, 0x01) => "aptX",
            (0x00D7, 0x24) => "aptX HD",
            (0x004F, 0x02) => "aptX Low Latency",
            (0x00D7, 0xAD) => "aptX Adaptive",
            (0x012D, 0xAA) => "LDAC",
            (0x0075, _) => "Samsung Scalable",
            _ => $"Vendor (0x{VendorId:X4}/0x{VendorCodecId:X2})"
        };
    }

    public int GetEstimatedBitrate()
    {
        return ToBluetoothCodec() switch
        {
            BluetoothCodec.SBC => 328,
            BluetoothCodec.AAC => 256,
            BluetoothCodec.AptX => 352,
            BluetoothCodec.AptXHD => 576,
            BluetoothCodec.AptXLL => 352,
            BluetoothCodec.AptXAdaptive => 420,
            BluetoothCodec.LDAC => 660,
            _ => 328
        };
    }

    public override string ToString()
    {
        return $"{GetCodecName()} ({GetEstimatedBitrate()} kbps)";
    }
}

public interface IBluetoothCodecMonitor : IDisposable
{
    event EventHandler<BluetoothA2DPCodecInfo>? CodecDetected;

    BluetoothA2DPCodecInfo? CurrentCodec { get; }

    void Start();

    void Stop();

    bool IsRunning { get; }

    bool RequiresElevation { get; }

    bool IsElevated { get; }
}

public class BluetoothCodecMonitor : IBluetoothCodecMonitor
{
    private static readonly ILogger Logger = Log.ForContext<BluetoothCodecMonitor>();

    private static readonly Guid BthA2dpETWProvider = Guid.Parse("8776ad1e-5022-4451-a566-f47e708b9075");
    private const string BthA2dpETWProviderName = "Microsoft.Windows.Bluetooth.BthA2dp";
    private const string A2dpStreamingEvent = "A2dpStreaming";
    private const string SessionName = "A2DPCommanderCodecSession";

    private TraceEventSession? _session;
    private Thread? _processingThread;
    private bool _isRunning;
    private bool _disposed;

    public event EventHandler<BluetoothA2DPCodecInfo>? CodecDetected;
    public BluetoothA2DPCodecInfo? CurrentCodec { get; private set; }
    public bool IsRunning => _isRunning;
    public bool RequiresElevation => true;

    public bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public void Start()
    {
        if (_isRunning)
        {
            Logger.Warning("Bluetooth codec monitor is already running");
            return;
        }

        if (!IsElevated)
        {
            Logger.Warning("Cannot start ETW session without administrator privileges");
            return;
        }

        try
        {
            try
            {
                var existingSession = TraceEventSession.GetActiveSession(SessionName);
                existingSession?.Dispose();
            }
            catch
            {
            }

            _session = new TraceEventSession(SessionName);
            _session.Source.Dynamic.All += OnEvent;

            _session.EnableProvider(BthA2dpETWProvider, TraceEventLevel.Verbose);

            _isRunning = true;

            _processingThread = new Thread(ProcessEvents)
            {
                IsBackground = true,
                Name = "BluetoothCodecMonitor"
            };
            _processingThread.Start();

            Logger.Information("Bluetooth codec monitor started");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start Bluetooth codec monitor");
            _isRunning = false;
            throw;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        try
        {
            _isRunning = false;
            _session?.Stop();
            _processingThread?.Join(TimeSpan.FromSeconds(2));

            Logger.Information("Bluetooth codec monitor stopped");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error stopping Bluetooth codec monitor");
        }
    }

    private void ProcessEvents()
    {
        try
        {
            _session?.Source.Process();
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Logger.Error(ex, "Error processing ETW events");
            }
        }
    }

    private void OnEvent(TraceEvent data)
    {
        try
        {
            if (!string.Equals(data.TaskName, A2dpStreamingEvent, StringComparison.OrdinalIgnoreCase))
                return;

            byte standardCodecId = 0;
            int vendorId = 0;
            int vendorCodecId = 0;

            try
            {
                if (data.PayloadNames.Length >= 6)
                {
                    var payload3 = data.PayloadValue(3);
                    var payload4 = data.PayloadValue(4);
                    var payload5 = data.PayloadValue(5);

                    if (payload3 != null) standardCodecId = Convert.ToByte(payload3);
                    if (payload4 != null) vendorId = Convert.ToInt32(payload4);
                    if (payload5 != null) vendorCodecId = Convert.ToInt32(payload5);
                }
            }
            catch
            {
                try
                {
                    var names = data.PayloadNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        var name = names[i];
                        if (name.Contains("StandardCodecId", StringComparison.OrdinalIgnoreCase))
                            standardCodecId = Convert.ToByte(data.PayloadValue(i));
                        else if (name.Contains("VendorId", StringComparison.OrdinalIgnoreCase) && !name.Contains("Codec"))
                            vendorId = Convert.ToInt32(data.PayloadValue(i));
                        else if (name.Contains("VendorCodecId", StringComparison.OrdinalIgnoreCase))
                            vendorCodecId = Convert.ToInt32(data.PayloadValue(i));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to parse A2DP streaming event payload by name");
                }
            }

            var codecInfo = new BluetoothA2DPCodecInfo
            {
                StandardCodecId = standardCodecId,
                VendorId = vendorId,
                VendorCodecId = vendorCodecId,
                DetectedAt = DateTime.Now
            };

            if (CurrentCodec == null ||
                CurrentCodec.StandardCodecId != codecInfo.StandardCodecId ||
                CurrentCodec.VendorId != codecInfo.VendorId ||
                CurrentCodec.VendorCodecId != codecInfo.VendorCodecId)
            {
                Logger.Information("Bluetooth A2DP codec detected: {Codec}", codecInfo);
                CurrentCodec = codecInfo;
                CodecDetected?.Invoke(this, codecInfo);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error processing ETW event");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
        _session?.Dispose();
    }
}
