using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Services.Settings
{
    /// <summary>
    /// Registry-backed repository for connection settings.
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
    [Service]
    public class ConnectionSettingsRepository
    {
        private const string ZonePrefix = "zone-";
        private const string VmPrefix = "vm-";

        private readonly IProjectRepository projectRepository;

        public ConnectionSettingsRepository(IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public ConnectionSettingsRepository(IServiceProvider serviceProvider)
            : this(serviceProvider.GetService<IProjectRepository>())
        {
        }

        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        public ProjectConnectionSettings GetProjectSettings(string projectId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(projectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(projectId);
                }

                return ProjectConnectionSettings.FromKey(projectId, key);
            }
        }

        public void SetProjectSettings(ProjectConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(settings.ProjectId))
            {
                if (key == null)
                {
                    throw new KeyNotFoundException(settings.ProjectId);
                }

                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        public ZoneConnectionSettings GetZoneSettings(string projectId, string zoneId)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                ZonePrefix + zoneId,
                true))
            {
                return ZoneConnectionSettings.FromKey(
                    projectId, 
                    zoneId, 
                    key);
            }
        }

        public void SetZoneSettings(ZoneConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                settings.ProjectId,
                ZonePrefix + settings.ZoneId,
                true))
            {
                settings.Save(key);
            }
        }

        //---------------------------------------------------------------------
        // Virtual Machines.
        //---------------------------------------------------------------------

        public VmInstanceConnectionSettings GetVmInstanceSettings(string projectId, string instanceName)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                VmPrefix + instanceName,
                true))
            {
                return VmInstanceConnectionSettings.FromKey(
                    projectId, 
                    instanceName, 
                    key);
            }
        }

        public void SetVmInstanceSettings(VmInstanceConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                settings.ProjectId,
                VmPrefix + settings.InstanceName,
                true))
            {
                settings.Save(key);
            }
        }
    }

    // TODO: move to separate file

    //
    // NB. The values do not map to RDP interface values. But the numeric values
    // must be kept unchanged as they are persisted in the registry.
    //

    public enum RdpConnectionBarState
    {
        AutoHide = 0,
        Pinned = 1,
        Off = 2,

        [Browsable(false)]
        _Default = AutoHide
    }

    public enum RdpDesktopSize
    {
        ClientSize = 0,
        ScreenSize = 1,
        AutoAdjust = 2,

        [Browsable(false)]
        _Default = AutoAdjust
    }

    public enum RdpAuthenticationLevel
    {
        // Likely to fail when using IAP unless the cert has been issued
        // for "localhost".
        AttemptServerAuthentication = 0,

        // Almsot guaranteed to fail, so do not even display it.
        [Browsable(false)]
        RequireServerAuthentication = 1,

        NoServerAuthentication = 3,

        [Browsable(false)]
        _Default = NoServerAuthentication
    }

    public enum RdpColorDepth
    {
        HighColor = 0,
        TrueColor = 1,
        DeepColor = 2,

        [Browsable(false)]
        _Default = TrueColor
    }

    public enum RdpAudioMode
    {
        PlayLocally = 0,
        PlayOnServer = 1,
        DoNotPlay = 2,

        [Browsable(false)]
        _Default = PlayLocally
    }

    public enum RdpRedirectClipboard
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Enabled
    }

    public enum RdpUserAuthenticationBehavior
    {
        PromptOnFailure = 0,
        AbortOnFailure = 1,

        [Browsable(false)]
        _Default = PromptOnFailure
    }

    public enum RdpBitmapPersistence
    {
        Disabled = 0,
        Enabled = 1,

        [Browsable(false)]
        _Default = Disabled
    }

    public enum RdpCredentialGenerationBehavior
    {
        Allow = 0,
        AllowIfNoCredentialsFound = 1,
        Disallow = 2,
        Force = 3,

        [Browsable(false)]
        _Default = AllowIfNoCredentialsFound
    }

    public abstract class ConnectionSettingsBase : IRegistrySettingsCollection
    {
        public RegistryStringSetting Username { get; private set; }
        public RegistrySecureStringSetting Password { get; private set; }
        public RegistryStringSetting Domain { get; private set; }
        public RegistryEnumSetting<RdpConnectionBarState> ConnectionBar { get; private set; }
        public RegistryEnumSetting<RdpDesktopSize> DesktopSize { get; private set; }
        public RegistryEnumSetting<RdpAuthenticationLevel> AuthenticationLevel { get; private set; }
        public RegistryEnumSetting<RdpColorDepth> ColorDepth { get; private set; }
        public RegistryEnumSetting<RdpAudioMode> AudioMode { get; private set; }
        public RegistryEnumSetting<RdpRedirectClipboard> RedirectClipboard { get; private set; }
        public RegistryEnumSetting<RdpUserAuthenticationBehavior> UserAuthenticationBehavior { get; private set; }
        public RegistryEnumSetting<RdpBitmapPersistence> BitmapPersistence { get; private set; }
        public RegistryDwordSetting ConnectionTimeout { get; private set; }
        public RegistryEnumSetting<RdpCredentialGenerationBehavior> CredentialGenerationBehavior { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.Username,
            this.Password,
            this.Domain,
            this.ConnectionBar,
            this.DesktopSize,
            this.AuthenticationLevel,
            this.ColorDepth,
            this.AudioMode,
            this.RedirectClipboard,
            this.UserAuthenticationBehavior,
            this.BitmapPersistence,
            this.ConnectionTimeout,
            this.CredentialGenerationBehavior,
        };

        protected void InitializeFromKey(RegistryKey key)
        {
            this.Username = RegistryStringSetting.FromKey(
                "Username",
                "Username",
                "Windows logon username",
                "Credentials",
                null,
                key,
                _ => true);
            this.Password = RegistrySecureStringSetting.FromKey(
                "Password",
                "Password",
                "Windows logon password",
                "Credentials",
                key,
                DataProtectionScope.CurrentUser);
            this.Domain = RegistryStringSetting.FromKey(
                "Domain",
                "Domain",
                "Windows logon domain",
                "Credentials",
                null,
                key,
                _ => true);
            this.ConnectionBar = RegistryEnumSetting<RdpConnectionBarState>.FromKey(
                "ConnectionBar",
                "Show connection bar",
                "Show connection bar in full-screen mode",
                "Display",
                RdpConnectionBarState._Default,
                key);
            this.DesktopSize = RegistryEnumSetting<RdpDesktopSize>.FromKey(
                "DesktopSize",
                "Desktop size",
                "Size of remote desktop",
                "Display",
                RdpDesktopSize._Default,
                key);
            this.AuthenticationLevel = RegistryEnumSetting<RdpAuthenticationLevel>.FromKey(
                "AuthenticationLevel",
                "Server authentication",
                "Require server authentication when connecting",
                "Connection",
                RdpAuthenticationLevel._Default,
                key);
            this.ColorDepth = RegistryEnumSetting<RdpColorDepth>.FromKey(
                "ColorDepth",
                "Color depth",
                "Color depth of remote desktop",
                "Display",
                RdpColorDepth._Default,
                key);
            this.AudioMode = RegistryEnumSetting<RdpAudioMode>.FromKey(
                "AudioMode",
                "Audio mode",
                "Redirect audio when playing on server",
                "Local resources",
                RdpAudioMode._Default,
                key);
            this.RedirectClipboard = RegistryEnumSetting<RdpRedirectClipboard>.FromKey(
                "RedirectClipboard",
                "Redirect clipboard",
                "Allow clipboard contents to be shared with remote desktop",
                "Local resources",
                RdpRedirectClipboard._Default,
                key);
            this.UserAuthenticationBehavior = RegistryEnumSetting<RdpUserAuthenticationBehavior>.FromKey(
                "RdpUserAuthenticationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                RdpUserAuthenticationBehavior._Default,
                key);
            this.BitmapPersistence = RegistryEnumSetting<RdpBitmapPersistence>.FromKey(
                "BitmapPersistence",
                "Bitmap caching",
                "Use persistent bitmap cache",
                "Performance",
                RdpBitmapPersistence._Default,
                key);
            this.ConnectionTimeout = RegistryDwordSetting.FromKey(
                "ConnectionTimeout",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                30,
                key,
                0, 300);
            this.CredentialGenerationBehavior = RegistryEnumSetting<RdpCredentialGenerationBehavior>.FromKey(
                "CredentialGenerationBehavior",
                null, // Hidden.
                null, // Hidden.
                null, // Hidden.
                RdpCredentialGenerationBehavior._Default,
                key);

            Debug.Assert(this.Settings.All(s => s != null));
        }
    }

    //-------------------------------------------------------------------------
    // VM instance.
    //-------------------------------------------------------------------------

    public class VmInstanceConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }
        public string InstanceName { get; }

        private VmInstanceConnectionSettings(string projectId, string instanceName)
        {
            this.ProjectId = projectId;
            this.InstanceName = instanceName;
        }

        public static VmInstanceConnectionSettings FromKey(
            string projectId, 
            string instanceName,
            RegistryKey registryKey)
        {

            var settings = new VmInstanceConnectionSettings(projectId, instanceName);
            settings.InitializeFromKey(registryKey);
            return settings;
        }

        public static VmInstanceConnectionSettings FromUrl(IapRdpUrl url)
        {
            var settings = FromKey(
                url.Instance.ProjectId, 
                url.Instance.Name,
                null);  // Apply defaults.
            
            // TODO: Apply values from URL parameters.
            //settings.ApplyValues(
            //    url.Parameters,
            //    true);

            // Never allow the password to be set by a URL parameter.
            settings.Password.Reset();
            return settings;
        }
    }

    //-------------------------------------------------------------------------
    // Zone.
    //-------------------------------------------------------------------------

    public class ZoneConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }
        public string ZoneId { get; }

        private ZoneConnectionSettings(string projectId, string zoneId)
        {
            this.ProjectId = projectId;
            this.ZoneId = zoneId;
        }

        public static ZoneConnectionSettings FromKey(
            string projectId,
            string zoneId,
            RegistryKey registryKey)
        {

            var settings = new ZoneConnectionSettings(projectId, zoneId);
            settings.InitializeFromKey(registryKey);
            return settings;
        }
    }

    //-------------------------------------------------------------------------
    // Project.
    //-------------------------------------------------------------------------

    public class ProjectConnectionSettings : ConnectionSettingsBase
    {
        public string ProjectId { get; }

        private ProjectConnectionSettings(string projectId)
        {
            this.ProjectId = projectId;
        }

        public static ProjectConnectionSettings FromKey(
            string ProjectId,
            RegistryKey registryKey)
        {

            var settings = new ProjectConnectionSettings(ProjectId);
            settings.InitializeFromKey(registryKey);
            return settings;
        }
    }
}
