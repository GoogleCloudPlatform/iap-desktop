using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.SettingsEditor
{
    public interface ISettingsEditor
    {
        void ShowWindow(ISettingsObject settingsObject);
    }

    public class BrowsableSettingAttribute : Attribute
    {
    }

    public interface ISettingsObject
    {
        void SaveChanges();
    }
}
