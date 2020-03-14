using Google.Solutions.CloudIap.IapDesktop.Application.Registry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Settings
{
    public abstract class SettingsEditorBase
    {
        protected readonly SettingsEditorBase parent;
        protected readonly InventorySettingsBase settings;

        public SettingsEditorBase(SettingsEditorBase parent, InventorySettingsBase settings)
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
        public string Username
        {
            get => this.settings.Username ?? this.parent?.Username;
            set => this.settings.Username = value;
        }

        public bool ShouldSerializeUsername() => this.settings.Username != null;

        [Browsable(true)]
        [Category("Credentials")]
        [DisplayName("Password")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get => this.settings.Password != null
                ? new string('*', this.settings.Password.Length)
                : this.parent.Password;
            set => this.settings.Password = SecureStringExtensions.FromClearText(value);
        }

        public bool ShouldSerializePassword() => this.settings.Password != null;

        [Browsable(true)]
        [Category("Credentials")]
        [DisplayName("Domain")]
        public string Domain
        {
            get => this.settings.Domain ?? this.parent?.Domain;
            set => this.settings.Domain = value;
        }

        public bool ShouldSerializeDomain() => this.settings.Domain != null;
    }


    public class GlobalSettingsEditor : SettingsEditorBase
    {
        public GlobalSettingsEditor(InventorySettings settings) : base(null, settings)
        { }
    }

    public class ProjectSettingsEditor : SettingsEditorBase
    {
        public ProjectSettingsEditor(GlobalSettingsEditor parent, ProjectSettings settings)
            : base(parent, settings)
        { }
    }

    public class RegionSettingsEditor : SettingsEditorBase
    {
        public RegionSettingsEditor(ProjectSettingsEditor parent, RegionSettings settings)
            : base(parent, settings)
        { }
    }

    public class ZoneSettingsEditor : SettingsEditorBase
    {
        public ZoneSettingsEditor(RegionSettingsEditor parent, ZoneSettings settings)
            : base(parent, settings)
        { }
    }

    public class VirtualMachineSettingsEditor : SettingsEditorBase
    {
        public VirtualMachineSettingsEditor(ZoneSettingsEditor parent, VirtualMachineSettings settings)
            : base(parent, settings)
        { }
    }

}
