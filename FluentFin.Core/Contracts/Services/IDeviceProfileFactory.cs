using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services
{
    public interface IDeviceProfileFactory
    {
        DeviceProfile GetDeviceProfile();
    }
}
