using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.Properties
{
    internal class SettingsCollectionTypeDescriptor : CustomTypeDescriptor
    {
        public ISettingsCollection Target { get; }

        private static ICustomTypeDescriptor GetTypeDescriptor(object obj)
        {
            var type = obj.GetType();
            var provider = TypeDescriptor.GetProvider(type);
            return provider.GetTypeDescriptor(type, obj);
        }

        public SettingsCollectionTypeDescriptor(ISettingsCollection target)
            : base(GetTypeDescriptor(target))
        {
            this.Target = target;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return new PropertyDescriptorCollection(
                this.Target.Settings
                    .Where(s => s.Title != null)
                    .Select(s =>
                    {
                        if (s is ISetting<SecureString> secureStringSetting)
                        {
                            return new SecureStringSettingDescriptor(secureStringSetting);
                        }
                        else
                        {
                            return new SettingDescriptor(s);
                        }
                    })
                    .ToArray());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            => GetProperties();

        private class SettingDescriptor : PropertyDescriptor
        {
            private readonly ISetting setting;

            public SettingDescriptor(ISetting setting)
                : base(setting.Key, null)
            {
                this.setting = setting;
            }

            public override string Name => this.setting.Key;
            public override string DisplayName => this.setting.Title;
            public override string Description => this.setting.Description;
            public override string Category => this.setting.Category;
            public override bool IsBrowsable => true;

            public override Type ComponentType => null;

            public override bool IsReadOnly => false;

            public override Type PropertyType => this.setting.ValueType;

            public override bool CanResetValue(object component)
            {
                return true;
            }

            public override object GetValue(object component)
            {
                return this.setting.Value;
            }

            public override void ResetValue(object component)
            {
                this.setting.Reset();
            }

            public override void SetValue(object component, object value)
            {
                this.setting.Value = value;
            }

            public override bool ShouldSerializeValue(object component)
            {
                return !this.setting.IsDefault;
            }
        }

        private class SecureStringSettingDescriptor : SettingDescriptor
        {
            private readonly ISetting<SecureString> setting;

            public SecureStringSettingDescriptor(ISetting<SecureString> setting)
                : base(setting)
            {
                this.setting = setting;
            }

            public override Type PropertyType => typeof(string);

            // Mask value as password.
            public override AttributeCollection Attributes
                => new AttributeCollection(new PasswordPropertyTextAttribute(true));

            public override object GetValue(object component)
            {
                return this.setting.IsDefault
                    ? null
                    : "********";
            }

            public override void SetValue(object component, object value)
            {
                //
                // NB. Avoid converting null to a SecureString as this results
                // in an empty string - which would then be treated as non-default.
                //

                if (value == null)
                {
                    ResetValue(component);
                }
                else
                {
                    this.setting.Value = SecureStringExtensions.FromClearText((string)value);
                }
            }
        }
    }
}
