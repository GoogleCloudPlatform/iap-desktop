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

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public interface IRegistrySetting : ISetting
    {
        RegistryValueKind Kind { get; }
    }

    public interface IRegistrySettingsCollection : ISettingsCollection
    {
    }

    public class RegistryStringSetting : SettingBase<string>, IRegistrySetting
    {
        private readonly Func<string, bool> validate;

        public RegistryValueKind Kind => RegistryValueKind.String;

        private RegistryStringSetting(
            string key,
            string title,
            string description,
            string category,
            string defaultValue,
            string value,
            Func<string, bool> validate)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue)
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
            => new RegistryStringSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  (string)backingKey.GetValue(key, defaultValue),
                  validate);

        protected override SettingBase<string> CreateNew(string value, string defaultValue)
            => new RegistryStringSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                this.validate);

        protected override bool IsValid(string value) => validate(value);

        protected override string Parse(string value) => value;
    }

    public class RegistryBoolSetting : SettingBase<bool>, IRegistrySetting
    {
        public RegistryValueKind Kind => RegistryValueKind.DWord;

        private RegistryBoolSetting(
            string key,
            string title,
            string description,
            string category,
            bool defaultValue,
            bool value)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue)
        {
        }

        public static RegistryBoolSetting FromKey(
            string key,
            string title,
            string description,
            string category,
            bool defaultValue,
            RegistryKey backingKey)
            => new RegistryBoolSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  (int?)backingKey.GetValue(key, defaultValue ? 1 : 0) != 0);

        protected override SettingBase<bool> CreateNew(bool value, bool defaultValue)
            => new RegistryBoolSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value);

        protected override bool IsValid(bool value) => true;

        protected override bool Parse(string value) 
            => value != null && bool.Parse(value);
    }

    public class RegistryDwordSetting : SettingBase<int>, IRegistrySetting
    {
        private readonly int minInclusive;
        private readonly int maxInclusive;

        public RegistryValueKind Kind => RegistryValueKind.DWord;

        private RegistryDwordSetting(
            string key,
            string title,
            string description,
            string category,
            int defaultValue,
            int value,
            int minInclusive,
            int maxInclusive)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue)
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
            => new RegistryDwordSetting(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  (int)backingKey.GetValue(key, defaultValue),
                  minInclusive,
                  maxInclusive);

        protected override SettingBase<int> CreateNew(int value, int defaultValue)
            => new RegistryDwordSetting(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value,
                this.minInclusive,
                this.maxInclusive);

        protected override bool IsValid(int value)
            => value >= this.minInclusive && value <= this.maxInclusive;

        protected override int Parse(string value) => int.Parse(value);
    }

    public class RegistryEnumSetting<TEnum> : SettingBase<TEnum>, IRegistrySetting
        where TEnum : struct
    {
        public RegistryValueKind Kind => RegistryValueKind.DWord;

        public RegistryEnumSetting(
            string key,
            string title,
            string description,
            string category,
            TEnum defaultValue,
            TEnum value)
            : base(
                  key,
                  title,
                  description,
                  category,
                  value,
                  defaultValue)
        {
        }

        public static RegistryEnumSetting<TEnum> FromKey(
            string key,
            string title,
            string description,
            string category,
            TEnum defaultValue,
            RegistryKey backingKey)
            => new RegistryEnumSetting<TEnum>(
                  key,
                  title,
                  description,
                  category,
                  defaultValue,
                  (TEnum)backingKey.GetValue(key, defaultValue));

        protected override SettingBase<TEnum> CreateNew(TEnum value, TEnum defaultValue)
            => new RegistryEnumSetting<TEnum>(
                this.Key,
                this.Title,
                this.Description,
                this.Category,
                defaultValue,
                value);

        protected override bool IsValid(TEnum value)
            => Enum.IsDefined(typeof(TEnum), value);

        protected override TEnum Parse(string value)
            => (TEnum)(object)int.Parse(value);
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
                backingKey.DeleteValue(setting.Key);
            }
            else
            {
                backingKey.SetValue(setting.Key, setting.Value, setting.Kind);
            }
        }

        public static void Save(
            this IRegistrySettingsCollection collection, 
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
