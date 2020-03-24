using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Windows;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WindowTestFixtureBase
    {

    }

    internal static class ControlTestExtensions
    {
        public static T GetChild<T>(this Control control, string name) where T : Control
        {
            if (control.Controls.ContainsKey(name))
            {
                return (T)control.Controls[name];
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Control {control.Name} does not have a child control named {name}");
            }
        }
    }
}
