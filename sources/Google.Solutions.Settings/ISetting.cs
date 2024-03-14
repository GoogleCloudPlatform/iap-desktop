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
using System;
using System.Security;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Base interface for a setting.
    /// </summary>
    public interface ISetting
    {
        /// <summary>
        /// Unique, stable key.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Name of setting, suitable for displaying.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Description of setting, suitable for displaying.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category of setting, suitable for displaying.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Determines whether the current value is equivalent to
        /// the default value.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Reset value to default.
        /// </summary>
        void Reset();

        /// <summary>
        /// Determines if the value has been changed and needs
        /// to be written back to the repository.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Determines whether the user has modified this setting.
        /// </summary>
        bool IsSpecified { get; }

        /// <summary>
        /// Determines if the user is allowed to change the setting
        /// (or whether it's mandated by a policy).
        /// </summary>
        bool IsReadOnly { get; }
    }

    public interface IAnySetting : ISetting // TODO: make internal
    {
        /// <summary>
        /// Assign value, converting data types if necessary.
        /// </summary>
        object AnyValue { get; set; }

        /// <summary>
        /// Returns the type of setting.
        /// </summary>
        Type ValueType { get; }
    }

    public interface ISetting<T> : ISetting, IAnySetting
    {
        /// <summary>
        /// Return current value of setting.
        /// </summary>
        T Value { get; set; }

        /// <summary>
        /// Returns the default value, which might be inherited.
        /// </summary>
        T DefaultValue { get; }
    }

    public static class SecureStringSettingExtensions
    {
        public static string GetClearTextValue(
            this ISetting<SecureString> setting)
        {
            return setting.Value?.AsClearText();
        }

        public static void SetClearTextValue(
            this ISetting<SecureString> setting,
            string value)
        {
            setting.Value = SecureStringExtensions.FromClearText(value);
        }
    }
}
