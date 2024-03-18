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
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Google.Solutions.Settings.ComponentModel;
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
                    .Where(s => s.DisplayName != null)
                    .Select(s =>
                    {
                        if (s is ISetting<SecureString> secureStringSetting)
                        {
                            return new MaskedSettingDescriptor(secureStringSetting);
                        }
                        else
                        {
                            return new SettingDescriptor((IAnySetting)s);
                        }
                    })
                    .ToArray());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }
    }
}
