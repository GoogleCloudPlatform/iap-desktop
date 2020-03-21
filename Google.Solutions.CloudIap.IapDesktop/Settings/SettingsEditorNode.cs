using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Settings
{
    public abstract class SettingsEditorNode
    {
        protected readonly SettingsEditorNode parent;
        protected readonly InventorySettingsBase settings;

        public SettingsEditorNode(SettingsEditorNode parent, InventorySettingsBase settings)
        {
            this.parent = parent;
            this.settings = settings;
        }

        //---------------------------------------------------------------------
        // Credentials.
        //---------------------------------------------------------------------

        [Browsable(true)]
        [Category("Credentials")]
        [DisplayName("Username")]
        [Description("Windows logon username")]
        public string Username
        {
            get => this.settings.Username ?? this.parent?.Username;
            set => this.settings.Username = value;
        }

        public bool ShouldSerializeUsername() => this.settings.Username != null;


        [Browsable(true)]
        [Category("Credentials")]
        [DisplayName("Password")]
        [Description("Windows logon password")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get => ShouldSerializePassword()
                ? new string('*', this.settings.Password.Length)
                : this.parent?.Password;
            set => this.settings.Password = SecureStringExtensions.FromClearText(value);
        }

        public bool ShouldSerializePassword() => this.settings.Password != null;


        [Browsable(true)]
        [Category("Credentials")]
        [DisplayName("Domain")]
        [Description("Windows logon domain")]
        public string Domain
        {
            get => this.settings.Domain ?? this.parent?.Domain;
            set => this.settings.Domain = value;
        }

        public bool ShouldSerializeDomain() => this.settings.Domain != null;


        [Browsable(true)]
        [Category("Display")]
        [DisplayName("Show connection bar")]
        [Description("Show connection bar in full-screen mode")]
        public RdpConnectionBarState ConnectionBar
        {
            get => ShouldSerializeConnectionBar()
                ? this.settings.ConnectionBar
                : (this.parent != null ? this.parent.ConnectionBar : RdpConnectionBarState._Default);
            set => this.settings.ConnectionBar = value;
        }

        public bool ShouldSerializeConnectionBar() 
            => this.settings.ConnectionBar != RdpConnectionBarState._Default;


        [Browsable(true)]
        [Category("Display")]
        [DisplayName("Desktop size")]
        [Description("Size of remote desktop")]
        public RdpDesktopSize DesktopSize
        {
            get => ShouldSerializeDesktopSize()
                ? this.settings.DesktopSize
                : (this.parent != null ? this.parent.DesktopSize : RdpDesktopSize._Default);
            set => this.settings.DesktopSize = value;
        }

        public bool ShouldSerializeDesktopSize()
            => this.settings.DesktopSize != RdpDesktopSize._Default;


        [Browsable(true)]
        [Category("Display")]
        [DisplayName("Color depth")]
        [Description("Color depth of remote desktop")]
        public RdpColorDepth ColorDepth
        {
            get => ShouldSerializeColorDepth()
                ? this.settings.ColorDepth
                : (this.parent != null ? this.parent.ColorDepth : RdpColorDepth._Default);
            set => this.settings.ColorDepth = value;
        }

        public bool ShouldSerializeColorDepth()
            => this.settings.ColorDepth != RdpColorDepth._Default;


        [Browsable(true)]
        [Category("Connection")]
        [DisplayName("Server authentication")]
        [Description("Require server authentication when connecting")]
        public RdpAuthenticationLevel AuthenticationLevel
        {
            get => ShouldSerializeAuthenticationLevel()
                ? this.settings.AuthenticationLevel
                : (this.parent != null ? this.parent.AuthenticationLevel : RdpAuthenticationLevel._Default);
            set => this.settings.AuthenticationLevel = value;
        }

        public bool ShouldSerializeAuthenticationLevel()
            => this.settings.AuthenticationLevel != RdpAuthenticationLevel._Default;


        [Browsable(true)]
        [Category("Local resources")]
        [DisplayName("Redirect clipboard")]
        [Description("Allow clipboard contents to be shared with remote desktop")]
        public bool RedirectClipboard
        {
            get => ShouldSerializeRedirectClipboard()
                ? this.settings.RedirectClipboard
                : (this.parent != null ? this.parent.RedirectClipboard : true);
            set => this.settings.RedirectClipboard = value;
        }

        public bool ShouldSerializeRedirectClipboard()
            => !this.settings.RedirectClipboard;


        [Browsable(true)]
        [Category("Local resources")]
        [DisplayName("Audio mode")]
        [Description("Redirect audio when playing on server")]
        public RdpAudioMode AudioMode
        {
            get => ShouldSerializeAudioMode()
                ? this.settings.AudioMode
                : (this.parent != null ? this.parent.AudioMode : RdpAudioMode._Default);
            set => this.settings.AudioMode = value;
        }

        public bool ShouldSerializeAudioMode()
            => this.settings.AudioMode != RdpAudioMode._Default;

    }


    public class GlobalSettingsEditorNode : SettingsEditorNode
    {
        public GlobalSettingsEditorNode(InventorySettings settings) : base(null, settings)
        { }
    }

    public class ProjectSettingsEditorNode : SettingsEditorNode
    {
        public ProjectSettingsEditorNode(GlobalSettingsEditorNode parent, ProjectSettings settings)
            : base(parent, settings)
        { }
    }

    public class ZoneSettingsEditorNode : SettingsEditorNode
    {
        public ZoneSettingsEditorNode(ProjectSettingsEditorNode parent, ZoneSettings settings)
            : base(parent, settings)
        { }
    }

    public class VmInstanceSettingsEditorNode : SettingsEditorNode
    {
        public VmInstanceSettingsEditorNode(ZoneSettingsEditorNode parent, VirtualMachineSettings settings)
            : base(parent, settings)
        { }

        [Browsable(true)]
        [Category("Instance")]
        [DisplayName("Name")]
        public string InstanceName => ((VirtualMachineSettings)this.settings).InstanceName;
    }

}
