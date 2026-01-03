using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IBluetoothAdapterService
{
    List<BluetoothAdapterInfo> GetAllAdapters();

    BluetoothAdapterInfo? GetActiveAdapter();

    bool SetActiveAdapter(string deviceInstanceId);

    bool EnableAdapter(string deviceInstanceId);

    bool DisableAdapter(string deviceInstanceId);

    event EventHandler<BluetoothAdapterInfo>? AdapterChanged;
}
