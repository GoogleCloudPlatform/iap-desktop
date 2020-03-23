using System;

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
