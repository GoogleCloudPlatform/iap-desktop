using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                true))
            {
                return ProjectConnectionSettings.FromKey(projectId, key);
            }
        }

        public void SetProjectSettings(string projectId, ProjectConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                true))
            {
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
                return ZoneConnectionSettings.FromKey(zoneId, key);
            }
        }

        public void SetZoneSettings(string projectId, ZoneConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
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
                return VmInstanceConnectionSettings.FromKey(instanceName, key);
            }
        }

        public void SetVmInstanceSettings(string projectId, VmInstanceConnectionSettings settings)
        {
            using (var key = this.projectRepository.OpenRegistryKey(
                projectId,
                VmPrefix + settings.InstanceName,
                true))
            {
                settings.Save(key);
            }
        }
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
        }
    }

    //-------------------------------------------------------------------------
    // VM instance.
    //-------------------------------------------------------------------------

    public class VmInstanceConnectionSettings : ConnectionSettingsBase
    {
        public string InstanceName { get; }

        private VmInstanceConnectionSettings(string instanceName)
        {
            this.InstanceName = instanceName;
        }

        public static VmInstanceConnectionSettings FromKey(
            string VmInstanceId,
            RegistryKey registryKey)
        {

            var settings = new VmInstanceConnectionSettings(VmInstanceId);
            settings.InitializeFromKey(registryKey);
            return settings;
        }
    }

    //-------------------------------------------------------------------------
    // Zone.
    //-------------------------------------------------------------------------

    public class ZoneConnectionSettings : ConnectionSettingsBase
    {
        public string ZoneId { get; }

        private ZoneConnectionSettings(string zoneId)
        {
            this.ZoneId = zoneId;
        }

        public static ZoneConnectionSettings FromKey(
            string zoneId,
            RegistryKey registryKey)
        {

            var settings = new ZoneConnectionSettings(zoneId);
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
