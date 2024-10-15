//
// Copyright 2024 Google LLC
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
using Google.Solutions.Common.Util;
using System;
using System.ComponentModel;
using System.Security;

namespace Google.Solutions.Settings.ComponentModel
{
    /// <summary>
    /// Exposes a SecureString-typed setting as a masked
    /// property in the .NET component model.
    /// </summary>
    public class MaskedSettingDescriptor : SettingDescriptor
    {
        private readonly ISetting<SecureString> setting;

        public MaskedSettingDescriptor(ISetting<SecureString> setting)
            : base(setting)
        {
            this.setting = setting.ExpectNotNull(nameof(setting));
        }

        public override Type PropertyType
        {
            get => typeof(string);
        }

        public override AttributeCollection Attributes
        {
            //
            // Mask value as password.
            //
            get => new AttributeCollection(new PasswordPropertyTextAttribute(true));
        }

        public override object GetValue(object component)
        {
            var value = base.GetValue(component);
            if (value is SecureString secureString) 
            {
                //
                // Return masked value, retaining the original length.
                //
                return new string('*', secureString.Length);
            }
            else
            {
                return value;
            }
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
