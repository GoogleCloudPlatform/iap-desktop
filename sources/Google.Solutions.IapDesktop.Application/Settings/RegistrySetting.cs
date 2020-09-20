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
            RegistryKey backingKey,
            Func<string, bool> validate)
            : base(
                  key,
                  title,
                  description,
                  (string)backingKey.GetValue(key, defaultValue),
                  defaultValue)
        {
            this.validate = validate;
        }

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
            RegistryKey backingKey)
            : base(
                  key,
                  title,
                  description,
                  (int)backingKey.GetValue(key, defaultValue) != 0,
                  defaultValue)
        {
        }

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
            RegistryKey backingKey,
            int minInclusive,
            int maxInclusive)
            : base(
                  key,
                  title,
                  description,
                  (int)backingKey.GetValue(key, defaultValue),
                  defaultValue)
        {
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

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
            RegistryKey backingKey,
            Func<TEnum, bool> validate)
            : base(
                  key,
                  title,
                  description,
                  (TEnum)backingKey.GetValue(key, defaultValue),
                  defaultValue)
        {
        }

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
