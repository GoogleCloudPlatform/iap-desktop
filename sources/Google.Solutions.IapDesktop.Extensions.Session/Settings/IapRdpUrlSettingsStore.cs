using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.Settings;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Session.Settings
{
    /// <summary>
    /// Exposes URL parameters as settings.
    /// </summary>
    internal class IapRdpUrlSettingsStore : DictionarySettingsStore
    {
        public IapRdpUrlSettingsStore(IapRdpUrl url)
            : base(url
                .ExpectNotNull(nameof(url))
                .Parameters
                .ToKeyValuePairs()
                .Where(kvp => kvp.Key != null && kvp.Value != null)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase))
        {
        }
    }
}
