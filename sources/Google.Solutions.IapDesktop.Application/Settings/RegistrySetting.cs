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
                  defaultValue,
                  validate)
        {
        }
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
                  defaultValue,
                  v => Enum.IsDefined(typeof(TEnum), v))
        {
        }
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
