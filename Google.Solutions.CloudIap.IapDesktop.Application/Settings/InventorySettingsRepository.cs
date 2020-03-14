using Google.Apis.Util;
using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Settings
{
    /// <summary>
    /// Registry-backed repository for GCE inventory settings.
    /// 
    /// To simplify key managent, a flat structure is used:
    /// 
    /// [base-key]
    ///    + [project-id]           => values...
    ///      + region-[region-id]   => values...
    ///      + zone-[zone-id]       => values...
    ///      + vm-[instance-name]   => values...
    ///      
    /// </summary>
    public class InventorySettingsRepository : SettingsRepositoryBase<InventorySettings>
    {
        private const string RegionPrefix = "region-";
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        public InventorySettingsRepository(RegistryKey baseKey) : base(baseKey)
        {
            Utilities.ThrowIfNull(baseKey, nameof(baseKey));
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public IEnumerable<ProjectSettings> ListProjectSettings()
        {
            foreach (var projectId in this.baseKey.GetSubKeyNames())
            {
                yield return GetProjectSettings(projectId);
            }
        }

        public ProjectSettings GetProjectSettings(string projectId)
        {
            var settings = Get<ProjectSettings>(projectId);
            settings.ProjectId = projectId;
            return settings;
        }

        public void SetProjectSettings(ProjectSettings settings)
        {
            Set<ProjectSettings>(settings.ProjectId, settings);
        }

        public void DeleteProjectSettings(string projectId)
        {
            this.baseKey.DeleteSubKeyTree(projectId, false);
        }


        //---------------------------------------------------------------------
        // Regions.
        //---------------------------------------------------------------------

        public RegionSettings GetRegionSettings(string projectId, string regionId)
        {
            var settings = Get<RegionSettings>($@"{projectId}\{RegionPrefix}{regionId}");
            settings.RegionId = regionId;
            return settings;
        }

        public void SetRegionSettings(string projectId, RegionSettings settings)
        {
            Set<RegionSettings>($@"{projectId}\{RegionPrefix}{settings.RegionId}", settings);
        }


        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ZoneSettings GetZoneSettings(string projectId, string zoneId)
        {
            var settings = Get<ZoneSettings>($@"{projectId}\{ZonePrefix}{zoneId}");
            settings.ZoneId = zoneId;
            return settings;
        }

        public void SetZoneSettings(string projectId, ZoneSettings settings)
        {
            Set<ZoneSettings>($@"{projectId}\{ZonePrefix}{settings.ZoneId}", settings);
        }


        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public VirtualMachineSettings GetVirtualMachineSettings(string projectId, string instanceName)
        {
            var settings = Get<VirtualMachineSettings>($@"{projectId}\{VmPrefix}{instanceName}");
            settings.InstanceName = instanceName;
            return settings;
        }

        public void SetVirtualMachineSettings(string projectId, VirtualMachineSettings settings)
        {
            Set<VirtualMachineSettings>($@"{projectId}\{VmPrefix}{settings.InstanceName}", settings);
        }
    }

    public abstract class InventorySettingsBase
    {
        //---------------------------------------------------------------------
        // Credentials.
        //---------------------------------------------------------------------

        [StringRegistryValue("Username")]
        public string Username { get; set; }

        [SecureStringRegistryValue("Password", DataProtectionScope.CurrentUser)]
        public SecureString Password { get; set; }

        [StringRegistryValue("Domain")]
        public string Domain { get; set; }
    }

    public class VirtualMachineSettings : InventorySettingsBase
    {
        // NB. The values of the enums are so that 0 is a sane default.
        // The values do not map to RDP interface values.
        public enum RdpConnectionBarState
        {
            AutoHide = 0,
            Pinned = 1,
            Off = 2
        }

        public enum RdpDesktopSize
        {
            ClientSize = 0,
            ScreenSize = 1
        }

        public enum RdpAuthenticationLevel
        {
            AttemptServerAuthentication = 0,
            RequireServerAuthentication = 1
        }

        public enum RdpColorDepth
        {
            HighColor = 0,
            TrueColor = 1,
            DeepColor = 2
        }

        public enum RdpAudioMode
        {
            PlayLocally = 0,
            PlayOnServer = 1,
            DoNotPlay = 2
        }


        public string InstanceName { get; set; }

        public RdpConnectionBarState ConnectionBar { get; set; } 
            = RdpConnectionBarState.AutoHide;

        public RdpDesktopSize DesktopSize { get; set; } 
            = RdpDesktopSize.ClientSize;

        public RdpAuthenticationLevel AuthenticationLevel { get; set; } 
            = RdpAuthenticationLevel.AttemptServerAuthentication;

        public RdpColorDepth ColorDepth { get; set; }
            = RdpColorDepth.TrueColor;

        public RdpAudioMode AudioMode { get; set; }
            = RdpAudioMode.PlayLocally;

        public bool RedirectClipboard { get; set; } = true;
    }

    public class RegionSettings : InventorySettingsBase
    {
        public string RegionId { get; set; }
    }

    public class ZoneSettings : InventorySettingsBase
    {
        public string ZoneId { get; set; }
    }

    public class ProjectSettings : InventorySettingsBase
    {
        public string ProjectId { get; set; }
    }

    public class InventorySettings : InventorySettingsBase
    {
    }
}
