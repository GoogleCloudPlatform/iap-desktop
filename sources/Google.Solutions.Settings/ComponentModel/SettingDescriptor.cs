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

using Google.Solutions.Common.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Google.Solutions.Settings.ComponentModel
{
    /// <summary>
    /// Exposes a setting as a property in the .NET component model.
    /// 
    /// This class can be used to expose one or more settings in a
    /// PropertyGrid.
    /// </summary>
    public class SettingDescriptor<T> : PropertyDescriptor
    {
        protected ISetting<T> Setting { get; }

        public SettingDescriptor(ISetting<T> setting)
            : base(setting.Key, null)
        {
            this.Setting = setting.ExpectNotNull(nameof(setting));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        /// <summary>
        /// Name (or key) of the setting, used for persistence.
        /// </summary>
        public override string Name
        {
            get => this.Setting.Key;
        }

        /// <summary>
        /// Human-readable name of the setting.
        /// </summary>
        public override string DisplayName
        {
            get => this.Setting.DisplayName;
        }

        /// <summary>
        /// Description, optional.
        /// </summary>
        public override string Description
        {
            get => this.Setting.Description;
        }

        /// <summary>
        /// Category, optional.
        /// </summary>
        public override string Category
        {
            get => this.Setting.Category;
        }

        /// <summary>
        /// Check if this setting should be displayed in a UI.
        /// </summary>
        public override bool IsBrowsable
        {
            get => this.Setting.DisplayName != null;
        }

        /// <summary>
        /// Type of the component this property is bound to.
        /// </summary>
        public override Type ComponentType
        {
            get => typeof(ISetting<T>);
        }

        /// <summary>
        /// Check whether this setting is read-only.
        /// </summary>
        public override bool IsReadOnly
        {
            get => this.Setting.IsReadOnly;
        }

        /// <summary>
        /// Type of the property.
        /// </summary>
        public override Type PropertyType
        {
            get => this.Setting.ValueType;
        }

        public override bool CanResetValue(object component)
        {
            Debug.Assert(component == this.Setting);
            return true;
        }

        public override object GetValue(object component)
        {
            Debug.Assert(component == this.Setting);
            return this.Setting.AnyValue;
        }

        public override void ResetValue(object component)
        {
            Debug.Assert(component == this.Setting);
            this.Setting.Reset();
        }

        public override void SetValue(object component, object value)
        {
            Debug.Assert(component == this.Setting);
            this.Setting.AnyValue = value;
        }

        public override bool ShouldSerializeValue(object component)
        {
            Debug.Assert(component == this.Setting);
            return !this.Setting.IsDefault;
        }
    }
}
