using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public interface ISettingsCollection
    {
        IEnumerable<ISetting> Settings { get; }
    }

    public static class SettingsCollectionExtensions
    {
        private static IEnumerable<ISetting> Overlay(
            ISettingsCollection baseSettings,
            ISettingsCollection overlaySettings)
        {
            var overlaySettingsByKey = overlaySettings.Settings.ToDictionary(
                s => s.Key,
                s => s);

            foreach (var baseSetting in baseSettings.Settings)
            {
                if (overlaySettingsByKey.TryGetValue(
                    baseSetting.Key,
                    out var overlaySetting))
                {
                    // Setting exists in both collections => overlay.
                    overlaySettingsByKey.Remove(overlaySetting.Key);
                    yield return baseSetting.OverlayBy(overlaySetting);
                }
                else
                {
                    // Setting only exists in base collection => keep.
                    yield return baseSetting;
                }
            }

            foreach (var overlaySetting in overlaySettingsByKey.Values)
            {
                // Setting only exists in overlay => add
                yield return overlaySetting;
            }
        }

        public static ISettingsCollection OverlayBy(
            this ISettingsCollection baseSettings,
            ISettingsCollection overlaySettings)
        {
            return new OverlayCollection(Overlay(baseSettings, overlaySettings));
        }

        public static void ApplyValues(
            this ISettingsCollection settings,
            NameValueCollection values,
            bool ignoreFormatErrors)
        {
            foreach (var setting in settings.Settings)
            {
                var value = values.Get(setting.Key);
                if (value != null)
                {
                    try
                    {
                        setting.Value = value;
                    }
                    catch (FormatException) when (ignoreFormatErrors)
                    {
                        // Ignore, keeping the previous value.
                    }
                }
            }
        }

        private class OverlayCollection : ISettingsCollection
        {
            public IEnumerable<ISetting> Settings { get; }

            public OverlayCollection(IEnumerable<ISetting> settings)
            {
                this.Settings = settings;
            }
        }
    }
}
