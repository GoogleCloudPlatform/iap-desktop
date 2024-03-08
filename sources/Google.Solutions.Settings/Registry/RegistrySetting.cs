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
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.Settings.Registry
{
    public interface IRegistrySetting : ISetting
    {
        RegistryValueKind Kind { get; }
        object RegistryValue { get; }
    }


    public class RegistryStringSetting
        : SettingBase<string>, IRegistrySetting
    {
        private readonly Func<string, bool> validate;

        public RegistryValueKind Kind => RegistryValueKind.String;
        public object RegistryValue => this.Value;

        private RegistryStringSetting(
            string key,
            string title,
            string description,
            string category,
            string defaultValue,
            string value,
            bool isSpecified,
            Func<string, bool> validate,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
            this.validate = validate;
        }

        public static RegistryStringSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            string defaultValue,
            RegistryKey backingKey,
            Func<string, bool> validate)
        {
            var value = (string)backingKey?.GetValue(key);
            return new RegistryStringSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  value ?? defaultValue,
                  value != null,
                  validate,
                  false);
        }

        protected override SettingBase<string> CreateNew(string value, string defaultValue, bool readOnly)
            => new RegistryStringSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                value == defaultValue,
                this.validate,
                readOnly);

        protected override bool IsValid(string value) => this.validate(value);

        protected override string Parse(string value) => value;

        public string StringValue
        {
            get => (string)this.Value;
            set => this.Value = value;
        }

        public RegistryStringSetting ApplyPolicy(RegistryKey policyKey)
        {
            var policyValue = (string)policyKey?.GetValue(this.Key);
            return policyValue == null || !IsValid(policyValue)
                ? this
                : (RegistryStringSetting)CreateNew(
                    policyValue,
                    this.DefaultValue,
                    true);
        }
    }

    public class RegistrySecureStringSetting
        : SettingBase<SecureString>, IRegistrySetting, ISecureStringSetting
    {
        private readonly DataProtectionScope protectionScope;

        public RegistryValueKind Kind => RegistryValueKind.Binary;
        public object RegistryValue => Encrypt(
            this.Key,
            this.protectionScope,
            (SecureString)this.Value);

        private RegistrySecureStringSetting(
            string key,
            string title,
            string description,
            string category,
            SecureString defaultValue,
            SecureString value,
            bool isSpecified,
            DataProtectionScope protectionScope,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
            this.protectionScope = protectionScope;
        }

        public static RegistrySecureStringSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            RegistryKey backingKey,
            DataProtectionScope protectionScope)
        {
            var value = (byte[])backingKey?.GetValue(key);
            return new RegistrySecureStringSetting(
                  key,
                  title,
                  description,
                  category,
                  null,
                  Decrypt(
                      key,
                      protectionScope,
                      value),
                  value != null,
                  protectionScope,
                  false);
        }

        protected override SettingBase<SecureString> CreateNew(
            SecureString value,
            SecureString defaultValue,
            bool readOnly)
            => new RegistrySecureStringSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                value == defaultValue,
                this.protectionScope,
                readOnly);

        protected override bool IsValid(SecureString value) => true;

        protected override SecureString Parse(string value)
            => SecureStringExtensions.FromClearText(value);

        internal static byte[] Encrypt(
            string key,
            DataProtectionScope scope,
            SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            var plaintextString = secureString.AsClearText();

            return ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plaintextString),
                Encoding.UTF8.GetBytes(key), // Entropy
                scope);
        }

        private static SecureString Decrypt(
            string key,
            DataProtectionScope scope,
            byte[] blob)
        {
            try
            {
                if (blob == null)
                {
                    return null;
                }

                var plaintextString = Encoding.UTF8.GetString(
                    ProtectedData.Unprotect(
                        blob,
                        Encoding.UTF8.GetBytes(key), // Entropy
                        scope));

                return SecureStringExtensions.FromClearText(plaintextString);
            }
            catch (CryptographicException)
            {
                // Value cannot be decrypted. This can happen if it was
                // written by a different user or if the current user's
                // key has changed (for example, because its credentials
                // been reset on GCE).
                return null;
            }
        }

        public string ClearTextValue
        {
            get => ((SecureString)this.Value)?.AsClearText();
            set => this.Value = SecureStringExtensions.FromClearText(value);
        }
    }

    public class RegistryBoolSetting : SettingBase<bool>, IRegistrySetting
    {
        public RegistryValueKind Kind => RegistryValueKind.DWord;
        public object RegistryValue => this.Value;

        private RegistryBoolSetting(
            string key,
            string title,
            string description,
            string category,
            bool defaultValue,
            bool value,
            bool isSpecified,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
        }

        public static RegistryBoolSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            bool defaultValue,
            RegistryKey backingKey)
        {
            var value = (int?)backingKey?.GetValue(key);
            return new RegistryBoolSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  value == null
                    ? defaultValue
                    : value != 0,
                  value != null,
                  false);
        }

        protected override SettingBase<bool> CreateNew(bool value, bool defaultValue, bool readOnly)
            => new RegistryBoolSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                value == defaultValue,
                readOnly);

        protected override bool IsValid(bool value) => true;

        protected override bool Parse(string value)
            => value != null && bool.Parse(value);

        public bool BoolValue
        {
            get => (bool)this.Value;
            set => this.Value = value;
        }

        public RegistryBoolSetting ApplyPolicy(RegistryKey policyKey)
        {
            var policyValue = (int?)policyKey?.GetValue(this.Key);
            return policyValue == null
                ? this
                : (RegistryBoolSetting)CreateNew(
                    policyValue == 1,
                    this.DefaultValue,
                    true);
        }
    }

    public class RegistryDwordSetting : SettingBase<int>, IRegistrySetting
    {
        private readonly int minInclusive;
        private readonly int maxInclusive;

        public RegistryValueKind Kind => RegistryValueKind.DWord;
        public object RegistryValue => this.Value;

        private RegistryDwordSetting(
            string key,
            string title,
            string description,
            string category,
            int defaultValue,
            int value,
            bool isSpecified,
            int minInclusive,
            int maxInclusive,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        public static RegistryDwordSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            int defaultValue,
            RegistryKey backingKey,
            int minInclusive,
            int maxInclusive)
        {
            var value = (int?)backingKey?.GetValue(key);
            return new RegistryDwordSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  value != null
                    ? value.Value
                    : defaultValue,
                  value != null,
                  minInclusive,
                  maxInclusive,
                  false);
        }

        protected override SettingBase<int> CreateNew(int value, int defaultValue, bool readOnly)
            => new RegistryDwordSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                value == defaultValue,
                this.minInclusive,
                this.maxInclusive,
                readOnly);

        protected override bool IsValid(int value)
            => value >= this.minInclusive && value <= this.maxInclusive;

        protected override int Parse(string value) => int.Parse(value);

        public int IntValue
        {
            get => (int)this.Value;
            set => this.Value = value;
        }

        public RegistryDwordSetting ApplyPolicy(RegistryKey policyKey)
        {
            var policyValue = (int?)policyKey?.GetValue(this.Key);
            return policyValue == null || !IsValid(policyValue.Value)
                ? this
                : (RegistryDwordSetting)CreateNew(
                    policyValue.Value,
                    this.DefaultValue,
                    true);
        }
    }

    public class RegistryQwordSetting : SettingBase<long>, IRegistrySetting
    {
        private readonly long minInclusive;
        private readonly long maxInclusive;

        public RegistryValueKind Kind => RegistryValueKind.QWord;
        public object RegistryValue => this.Value;

        private RegistryQwordSetting(
            string key,
            string title,
            string description,
            string category,
            long defaultValue,
            long value,
            bool isSpecified,
            long minInclusive,
            long maxInclusive,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        public static RegistryQwordSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            long defaultValue,
            RegistryKey backingKey,
            long minInclusive,
            long maxInclusive)
        {
            var value = (long?)backingKey?.GetValue(key);
            return new RegistryQwordSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  value != null
                    ? value.Value
                    : defaultValue,
                  value != null,
                  minInclusive,
                  maxInclusive,
                  false);
        }

        protected override SettingBase<long> CreateNew(long value, long defaultValue, bool readOnly)
            => new RegistryQwordSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                value == defaultValue,
                this.minInclusive,
                this.maxInclusive,
                readOnly);

        protected override bool IsValid(long value)
            => value >= this.minInclusive && value <= this.maxInclusive;

        protected override long Parse(string value) => long.Parse(value);

        public long LongValue
        {
            get => (long)this.Value;
            set => this.Value = value;
        }

        public RegistryQwordSetting ApplyPolicy(RegistryKey policyKey)
        {
            var policyValue = (long?)policyKey?.GetValue(this.Key);
            return policyValue == null || !IsValid(policyValue.Value)
                ? this
                : (RegistryQwordSetting)CreateNew(
                    policyValue.Value,
                    this.DefaultValue,
                    true);
        }
    }

    public class RegistryEnumSetting<TEnum>
        : SettingBase<TEnum>, IRegistrySetting, IEnumSetting<TEnum>
        where TEnum : struct
    {
        public RegistryValueKind Kind => RegistryValueKind.DWord;
        public object RegistryValue => this.Value;

        public RegistryEnumSetting(
            string key,
            string title,
            string description,
            string category,
            TEnum defaultValue,
            TEnum value,
            bool isSpecified,
            bool readOnly)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue,
                  isSpecified,
                  readOnly)
        {
        }

        public static RegistryEnumSetting<TEnum> FromKey(
            string key,
            string title,
            string description,
            string category,
            TEnum defaultValue,
            RegistryKey backingKey)
        {
            var value = backingKey?.GetValue(key);
            return new RegistryEnumSetting<TEnum>(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  value != null
                    ? (TEnum)value
                    : defaultValue,
                  value != null,
                  false);
        }

        protected override SettingBase<TEnum> CreateNew(TEnum value, TEnum defaultValue, bool readOnly)
            => new RegistryEnumSetting<TEnum>(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                Equals(value, defaultValue),
                readOnly);

        protected override bool IsValid(TEnum value)
        {
            var numericValue = Convert.ToInt64(value);

            // Create a bit field with all flags on.
            var max = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Select(v => Convert.ToInt64(v))
                .Aggregate((e1, e2) => e1 | e2);

            return (max & numericValue) == numericValue;
        }

        protected override TEnum Parse(string value)
            => (TEnum)(object)int.Parse(value);

        public TEnum EnumValue
        {
            get => (TEnum)this.Value;
            set => this.Value = value;
        }

        public RegistryEnumSetting<TEnum> ApplyPolicy(RegistryKey policyKey)
        {
            var policyValue = (int?)policyKey?.GetValue(this.Key);
            return policyValue == null || !IsValid((TEnum)(object)policyValue.Value)
                ? this
                : (RegistryEnumSetting<TEnum>)CreateNew(
                    (TEnum)(object)policyValue.Value,
                    this.DefaultValue,
                    true);
        }
    }

    public static class RegistrySettingsExtensions
    {
        public static void Save(
            this IRegistrySetting setting,
            RegistryKey backingKey)
        {
            Debug.Assert(setting.IsDirty);
            if (setting.IsDefault)
            {
                backingKey.DeleteValue(setting.Key, false);
            }
            else
            {
                backingKey.SetValue(
                    setting.Key,
                    setting.RegistryValue,
                    setting.Kind);
            }
        }

        public static void Save(
            this ISettingsCollection collection,
            RegistryKey registryKey)
        {
            foreach (var setting in collection.Settings
                .Where(s => s.IsDirty)
                .Cast<IRegistrySetting>())
            {
                setting.Save(registryKey);
            }
        }
    }
}
