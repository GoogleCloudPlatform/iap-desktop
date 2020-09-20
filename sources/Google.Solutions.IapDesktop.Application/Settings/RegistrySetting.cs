using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Google.Solutions.IapDesktop.Application.Settings
{

    public interface IRegistrySetting : ISetting
    {
    }

    public interface IRegistrySettingsCollection : ISettingsCollection
    {
    }

    public class RegistryStringSetting : SettingBase<string>, IRegistrySetting
    {
        private readonly Func<string, bool> validate;

        private RegistryStringSetting(
            string key,
            string title,
            string description,
            string defaultValue,
            string value,
            Func<string, bool> validate)
            : base(
                  key,
                  title,
                  description,
                  value,
                  defaultValue)
        {
            this.validate = validate;
        }

        public RegistryStringSetting FromKey(
            string key,
            string title,
            string description,
            string defaultValue,
            RegistryKey backingKey,
            Func<string, bool> validate)
            => new RegistryStringSetting(
                  key,
                  title,
                  description,
                  (string)backingKey.GetValue(key, defaultValue),
                  defaultValue,
                  validate);

        protected override SettingBase<string> CreateNew(string value, string defaultValue)
            => new RegistryStringSetting(
                this.Key,
                this.Title,
                this.Description,
                defaultValue,
                value,
                this.validate);

        protected override bool IsValid(string value) => validate(value);

        protected override string Parse(string value) => value;
    }

    public class RegistryBoolSetting : SettingBase<bool>, IRegistrySetting
    {
        private RegistryBoolSetting(
            string key,
            string title,
            string description,
            bool defaultValue,
            bool value)
            : base(
                  key,
                  title,
                  description,
                  value,
                  defaultValue)
        {
        }

        public RegistryBoolSetting FromKey(
            string key,
            string title,
            string description,
            bool defaultValue,
            RegistryKey backingKey)
            => new RegistryBoolSetting(
                  key,
                  title,
                  description,
                  (int)backingKey.GetValue(key, defaultValue) != 0,
                  defaultValue);

        protected override SettingBase<bool> CreateNew(bool value, bool defaultValue)
            => new RegistryBoolSetting(
                this.Key,
                this.Title,
                this.Description,
                defaultValue,
                value);

        protected override bool IsValid(bool value) => true;

        protected override bool Parse(string value) => bool.Parse(value);
    }

    public class RegistryDwordSetting : SettingBase<int>, IRegistrySetting
    {
        private readonly int minInclusive;
        private readonly int maxInclusive;

        private RegistryDwordSetting(
            string key,
            string title,
            string description,
            int defaultValue,
            int value,
            int minInclusive,
            int maxInclusive)
            : base(
                  key,
                  title,
                  description,
                  value,
                  defaultValue)
        {
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        public RegistryDwordSetting FromKey(
            string key,
            string title,
            string description,
            int defaultValue,
            RegistryKey backingKey,
            int minInclusive,
            int maxInclusive)
            => new RegistryDwordSetting(
                  key,
                  title,
                  description,
                  (int)backingKey.GetValue(key, defaultValue),
                  defaultValue,
                  minInclusive,
                  maxInclusive);

        protected override SettingBase<int> CreateNew(int value, int defaultValue)
            => new RegistryDwordSetting(
                this.Key,
                this.Title,
                this.Description,
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
        public RegistryEnumSetting(
            string key,
            string title,
            string description,
            TEnum defaultValue,
            TEnum value)
            : base(
                  key,
                  title,
                  description,
                  value,
                  defaultValue)
        {
        }

        public static RegistryEnumSetting<TEnum> FromKey(
            string key,
            string title,
            string description,
            TEnum defaultValue,
            RegistryKey backingKey)
            => new RegistryEnumSetting<TEnum>(
                  key,
                  title,
                  description,
                  (TEnum)backingKey.GetValue(key, defaultValue),
                  defaultValue);

        protected override SettingBase<TEnum> CreateNew(TEnum value, TEnum defaultValue)
            => new RegistryEnumSetting<TEnum>(
                this.Key,
                this.Title,
                this.Description,
                defaultValue,
                value);

        protected override bool IsValid(TEnum value)
            => Enum.IsDefined(typeof(TEnum), value);

        protected override TEnum Parse(string value)
            => (TEnum)(object)int.Parse(value);
    }

    public static class RegistrySettingsExtensions
    {
        public static void Save(this IRegistrySetting setting, RegistryKey backingKey)
        {
            Debug.Assert(setting.IsDirty);
            backingKey.SetValue(setting.Key, setting.Value, RegistryValueKind.DWord);
        }

        public static void Save(this IRegistrySettingsCollection collection, RegistryKey registryKey)
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
