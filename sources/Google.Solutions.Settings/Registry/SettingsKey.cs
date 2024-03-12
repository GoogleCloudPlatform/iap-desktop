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
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.Settings.Registry
{
    /// <summary>
    /// Exposes a registry key's values as settings.
    /// </summary>
    public class SettingsKey : IDisposable // TODO: Extract interface, also implement by IapUrl (and then remove Parse)
    {
        /// <summary>
        /// Delegate for validating if a given value falls
        /// within the permitted range of a setting.
        /// </summary>
        public delegate bool ValidateDelegate<T>(T value);

        /// <summary>
        /// Delegate for parsing a string and converting it
        /// into the setting type.
        /// </summary>
        protected delegate bool ParseDelegate<T>(string value, out T result);

        internal RegistryKey BackingKey { get; }

        public SettingsKey(RegistryKey key)
        {
            this.BackingKey = key.ExpectNotNull(nameof(key));
        }

        /// <summary>
        /// Read key value and map it to a settings object.
        /// </summary>
        public virtual ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null)
        {
            var defaultTypeMapping = GetDefaultTypeMapping<T>(name);
            var typeMapping = new TypeMapping<T>(
                defaultTypeMapping.Accessor,
                validate ?? defaultTypeMapping.Validate,
                defaultTypeMapping.Parse);

            bool isSpecified = defaultTypeMapping
                .Accessor
                .TryRead(this.BackingKey, out var readValue);

            return new MappedSetting<T>(
                name,
                displayName,
                description,
                category,
                isSpecified ? readValue : defaultValue,
                defaultValue,
                isSpecified,
                false,
                typeMapping);
        }

        /// <summary>
        /// Write value back to registry.
        /// </summary>
        public void Write(ISetting setting)
        {
            Debug.Assert(setting.IsDirty);
            Debug.Assert(!setting.IsReadOnly);

            ((IMappedSetting)setting).Write(this.BackingKey);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.BackingKey.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //---------------------------------------------------------------------
        // Type mapping.
        //---------------------------------------------------------------------

        private static TypeMapping<T> GetDefaultTypeMapping<T>(string valueName)
        {
            bool TryParseString(string input, out string output)
            {
                output = input;
                return true;
            }

            bool TryParseSecureString(string input, out SecureString output)
            {
                output = SecureStringExtensions.FromClearText(input);
                return true;
            }

            bool TryParseEnum<TEnum>(string input, out TEnum output)
            { 
                if (int.TryParse(input, out var intValue))
                {
                    output = (TEnum)(object)intValue;
                    return true;
                }
                else
                {
                    output = default(TEnum);
                    return false;
                }
            }

            if (typeof(T) == typeof(bool))
            {
                return (TypeMapping<T>)(object) new TypeMapping<bool>(
                    new BoolRegistryValueAccessor(valueName),
                    v => true,
                    bool.TryParse);
            }
            else if (typeof(T) == typeof(int))
            {
                return (TypeMapping<T>)(object)new TypeMapping<int>(
                    new DwordRegistryValueAccessor(valueName),
                    v => true,
                    int.TryParse);
            }
            else if (typeof(T) == typeof(long))
            {
                return (TypeMapping<T>)(object)new TypeMapping<long>(
                    new QwordRegistryValueAccessor(valueName),
                    v => true,
                    long.TryParse);
            }
            else if (typeof(T) == typeof(string))
            {
                return (TypeMapping<T>)(object)new TypeMapping<string>(
                    new StringRegistryValueAccessor(valueName),
                    v => true,
                    TryParseString);
            }
            else if (typeof(T) == typeof(SecureString))
            {
                return (TypeMapping<T>)(object)new TypeMapping<SecureString>(
                    new SecureStringRegistryValueAccessor(valueName, DataProtectionScope.CurrentUser),
                    v => true,
                    TryParseSecureString);
            }
            else if (typeof(T).IsEnum)
            {
                return (TypeMapping<T>)(object)new TypeMapping<T>(
                    new EnumRegistryValueAccessor<T>(valueName),
                    v => v.IsDefinedFlagCombination(),
                    TryParseEnum);
            }
            else
            {
                throw new ArgumentException(
                    $"Registry value cannot be mapped to a setting " +
                    $"of type {typeof(T).Name}");
            }
        }

        protected class TypeMapping<T>
        {
            internal RegistryValueAccessor<T> Accessor { get; }
            internal ValidateDelegate<T> Validate { get; }
            internal ParseDelegate<T> Parse { get; }

            internal TypeMapping(
                RegistryValueAccessor<T> accessor, 
                ValidateDelegate<T> defaultValidate, 
                ParseDelegate<T> defaultParse)
            {
                this.Accessor = accessor;
                this.Validate = defaultValidate;
                this.Parse = defaultParse;
            }
        }

        protected interface IMappedSetting
        {
            void Write(RegistryKey key);
        }

        protected class MappedSetting<T> : SettingBase<T>, IMappedSetting // TODO: Merge into SettingBase
        {
            private readonly TypeMapping<T> typeMapping;

            public MappedSetting(
                string key,
                string title,
                string description,
                string category,
                T initialValue,
                T defaultValue,
                bool isSpecified,
                bool readOnly,
                TypeMapping<T> typeMapping) 
                : base(key, 
                      title, 
                      description, 
                      category, 
                      initialValue,
                      defaultValue,
                      isSpecified, 
                      readOnly)
            {
                this.typeMapping = typeMapping.ExpectNotNull(nameof(typeMapping));
            }

            protected override SettingBase<T> CreateNew(
                T value, 
                T defaultValue, 
                bool readOnly) // TODO: remove 
            {
                return new MappedSetting<T>(
                    this.Key,
                    this.Title,
                    this.Description,
                    this.Category,
                    value,
                    defaultValue,
                    Equals(value, defaultValue),
                    readOnly,
                    this.typeMapping);
            }

            internal SettingBase<T> CreateSimilar(
                T value,
                T defaultValue,
                bool isSpecified,
                bool readOnly)
            {
                return new MappedSetting<T>(
                    this.Key,
                    this.Title,
                    this.Description,
                    this.Category,
                    value,
                    defaultValue,
                    isSpecified,
                    readOnly,
                    this.typeMapping);
            }

            protected override bool IsValid(T value)
            {
                return this.typeMapping.Validate(value);
            }

            protected override T Parse(string value)
            {
                if (this.typeMapping.Parse(value, out var result))
                {
                    return result;
                }
                else
                {
                    throw new FormatException("The input format is invalid");
                }
            }

            public void Write(RegistryKey key)
            {
                if (this.IsDefault)
                {
                    this.typeMapping.Accessor.Delete(key);
                }
                else
                {
                    this.typeMapping.Accessor.Write(key, this.Value);
                }
            }

            public bool IsCurrentValueValid
            {
                get => IsValid(this.Value);
            }
        }
    }
}
