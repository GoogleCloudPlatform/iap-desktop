using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    /// <summary>
    /// Registry-backed repository for UI layout settings.
    /// </summary>
    public class WindowSettingsRepository : SettingsRepositoryBase<WindowSettings>
    {
        public WindowSettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }
    }

    public class WindowSettings
    {
        [BoolRegistryValue("IsMainWindowMaximized")]
        public bool IsMainWindowMaximized { get; set; }

        [DwordRegistryValue("MainWindowHeight")]
        public int MainWindowHeight { get; set; }

        [DwordRegistryValue("WindowWidth")]
        public int MainWindowWidth { get; set; }
    }
}
