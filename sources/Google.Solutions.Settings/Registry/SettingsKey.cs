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
    /// Registry key that is used for storing settings.
    /// </summary>
    public class SettingsKey : IDisposable
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
        private delegate bool ParseDelegate<T>(string value, out T result);

        private readonly RegistryKey key;

        public SettingsKey(RegistryKey key)
        {
            this.key = key.ExpectNotNull(nameof(key));
        }

        /// <summary>
        /// Read key value and map it to a settings object.
        /// </summary>
        public ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null)
        {
            var mapping = GetTypeMapping<T>(name);

            bool isSpecified = mapping
                .Accessor
                .TryRead(this.key, out var initialValue);

            return new MappedSetting<T>(
                name,
                displayName,
                description,
                category,
                isSpecified ? initialValue : defaultValue,
                defaultValue,
                isSpecified,
                false,
                validate ?? mapping.DefaultValidate,
                mapping.DefaultParse);
        }

        /// <summary>
        /// Write value back to registry.
        /// </summary>
        public void Write<T>(ISetting<T> setting)
        {
            Debug.Assert(setting.IsDirty);

            var accessor = GetTypeMapping<T>(setting.Key).Accessor;
            if (setting.IsDefault)
            {
                accessor.Delete(this.key);
            }
            else
            {
                accessor.Write(this.key, setting.Value);
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.key.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //---------------------------------------------------------------------
        // Type mapping.
        //---------------------------------------------------------------------

        private static TypeMapping<T> GetTypeMapping<T>(string valueName)
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

            bool ValidateEnum<TEnum>(TEnum value)
            {
                var numericValue = Convert.ToInt64(value);

                //
                // Create a bit field with all flags on.
                //
                var max = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                    .Select(v => Convert.ToInt64(v))
                    .Aggregate((e1, e2) => e1 | e2);

                return (max & numericValue) == numericValue;
            }

            if (typeof(T) == typeof(bool))
            {
                return (TypeMapping<T>)(object) new TypeMapping<bool>(
                    new BoolValueAccessor(valueName),
                    v => true,
                    bool.TryParse);
            }
            else if (typeof(T) == typeof(int))
            {
                return (TypeMapping<T>)(object)new TypeMapping<int>(
                    new DwordValueAccessor(valueName),
                    v => true,
                    int.TryParse);
            }
            else if (typeof(T) == typeof(long))
            {
                return (TypeMapping<T>)(object)new TypeMapping<long>(
                    new QwordValueAccessor(valueName),
                    v => true,
                    long.TryParse);
            }
            else if (typeof(T) == typeof(string))
            {
                return (TypeMapping<T>)(object)new TypeMapping<string>(
                    new StringValueAccessor(valueName),
                    v => true,
                    TryParseString);
            }
            else if (typeof(T) == typeof(SecureString))
            {
                return (TypeMapping<T>)(object)new TypeMapping<SecureString>(
                    new SecureStringValueAccessor(valueName, DataProtectionScope.CurrentUser),
                    v => true,
                    TryParseSecureString);
            }
            else if (typeof(T).IsEnum)
            {
                return (TypeMapping<T>)(object)new TypeMapping<T>(
                    new EnumValueAccessor<T>(valueName),
                    ValidateEnum,
                    TryParseEnum);
            }
            else
            {
                throw new ArgumentException(
                    $"Registry value cannot be mapped to a setting " +
                    $"of type {typeof(T).Name}");
            }
        }

        private class TypeMapping<T>
        {
            public ValueAccessor<T> Accessor { get; }
            public ValidateDelegate<T> DefaultValidate { get; }
            public ParseDelegate<T> DefaultParse { get; }

            public TypeMapping(
                ValueAccessor<T> accessor, 
                ValidateDelegate<T> defaultValidate, 
                ParseDelegate<T> defaultParse)
            {
                this.Accessor = accessor;
                this.DefaultValidate = defaultValidate;
                this.DefaultParse = defaultParse;
            }
        }

        private class MappedSetting<T> : SettingBase<T>
        {
            private readonly ValidateDelegate<T> isValid;
            private readonly ParseDelegate<T> parse;

            public MappedSetting(
                string key,
                string title,
                string description,
                string category,
                T initialValue,
                T defaultValue,
                bool isSpecified,
                bool readOnly,
                ValidateDelegate<T> isValid,
                ParseDelegate<T> parse) 
                : base(key, 
                      title, 
                      description, 
                      category, 
                      initialValue,
                      defaultValue,
                      isSpecified, 
                      readOnly)
            {
                this.isValid = isValid.ExpectNotNull(nameof(isValid));
                this.parse = parse.ExpectNotNull(nameof(parse));
            }

            protected override SettingBase<T> CreateNew(
                T value, 
                T defaultValue, 
                bool readOnly) // TODO: remove parameter?
            {
                return new MappedSetting<T>(
                    this.Key,
                    this.Title,
                    this.Description,
                    this.Category,
                    defaultValue,
                    value,
                    Equals(value, defaultValue),
                    readOnly,
                    this.isValid,
                    this.parse);
            }

            protected override bool IsValid(T value)
            {
                return this.isValid(value);
            }

            protected override T Parse(string value)
            {
                if (this.parse(value, out var result))
                {
                    return result;
                }
                else
                {
                    throw new FormatException("The input format is invalid");
                }
            }
        }
    }
}
