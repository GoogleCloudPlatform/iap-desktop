//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security;

namespace Google.Solutions.IapDesktop.Application.ToolWindows.Properties
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
                            return new SettingDescriptor((IAnySetting)s);
                        }
                    })
                    .ToArray());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            => GetProperties();

        private class SettingDescriptor : PropertyDescriptor
        {
            private readonly IAnySetting setting;

            public SettingDescriptor(IAnySetting setting)
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
                return this.setting.AnyValue;
            }

            public override void ResetValue(object component)
            {
                this.setting.Reset();
            }

            public override void SetValue(object component, object value)
            {
                this.setting.AnyValue = value;
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
