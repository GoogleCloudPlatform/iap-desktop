using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface SecureConnectAdapter
    {
        Task<DeviceState> QueryDeviceStateAsync(string userId);
    }

    public class DeviceState
    {
        public bool IsEnrolled { get; }
        public string CertificateFingerprint { get; }
    }
}
