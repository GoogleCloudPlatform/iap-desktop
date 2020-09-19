using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public class SampleSettings : IRegistrySettingsCollection
    {
        public RegistryEnumSetting<ConsoleColor> Color { get; private set; }

        public IEnumerable<ISetting> Settings => new ISetting[]
        {
            this.Color
        };

        private SampleSettings()
        {
        }

        public static SampleSettings FromKey(RegistryKey registryKey)
        {
            return new SampleSettings()
            {
                Color = new RegistryEnumSetting<ConsoleColor>(
                    "Color",
                    "The color",
                    "some desc",
                    ConsoleColor.Red,
                    registryKey)
            };
        }
    }
}
