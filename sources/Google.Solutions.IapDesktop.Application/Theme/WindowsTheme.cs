using Google.Apis.Util;
using Google.Solutions.Common.Interop;
using Google.Solutions.Mvvm.Theme;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal abstract class WindowsTheme : IControlTheme
    {
        /// <summary>
        /// Check if the Windows version supports dark mode.
        /// </summary>
        public static bool IsDarkModeSupported
        {
            get
            {
                //
                // We're using UxTheme.dll, which is undocumented. The
                // export ordinals should be constant since this build, but
                // were different or missing before.
                //
                var osVersion = Environment.OSVersion.Version;
                return osVersion.Major > 10 ||
                       (osVersion.Major == 10 && osVersion.Build >= 18985);
            }
        }

        /// <summary>
        /// Check if Windows apps should use dark mode.
        /// </summary>
        public static bool IsDarkModeEnabled
        {
            get
            {
                if (!IsDarkModeSupported)
                {
                    return false;
                }

                //
                // Running on an OS version that supports dark mode, so
                // it should be safe to make this API call.
                //
                return NativeMethods.ShouldAppsUseDarkMode();
            }
        }

        public static WindowsTheme GetSystemTheme()
        {
            return IsDarkModeEnabled
                ? (WindowsTheme)new DarkTheme()
                : new ClassicTheme();
        }


        /// <summary>
        /// Return the default theme, which works on all OS versions.
        /// </summary>
        public static WindowsTheme GetDefaultTheme()
        {
            return new ClassicTheme();
        }

        /// <summary>
        /// Return the default theme, which works on all OS versions.
        /// </summary>
        public static WindowsTheme GetDarkTheme()
        {
            return new DarkTheme();
        }

        public virtual void ApplyTo(Control control)
        { }

        //---------------------------------------------------------------------
        // Implementations.
        //---------------------------------------------------------------------

        /// <summary>
        /// Classic, light theme.
        /// </summary>
        private class ClassicTheme : WindowsTheme
        {
        }

        /// <summary>
        /// Windows 10-style dark theme. See
        /// https://github.com/microsoft/WindowsAppSDK/issues/41 for details
        /// on undocumented method calls.
        /// </summary>
        private class DarkTheme : WindowsTheme
        {
            public DarkTheme()
            {
                //
                // Enable dark theme support for Win32 controls.
                //
                var ret = NativeMethods.SetPreferredAppMode(NativeMethods.APPMODE_ALLOWDARK);
                Debug.Assert(ret == 0);
            }

            public override void ApplyTo(Control control)
            {
                if (control is Form form)
                {
                    //
                    // Use dark title bar, see
                    // https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/apply-windows-themes#enable-a-dark-mode-title-bar-for-win32-applications
                    //
                    int darkMode = 1;
                    var hr = NativeMethods.DwmSetWindowAttribute(
                        form.Handle,
                        NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        ref darkMode,
                        sizeof(int));
                    if (hr != HRESULT.S_OK)
                    {
                        throw new Win32Exception(
                            "Updating window attributes failed");
                    }
                }
                
                NativeMethods.AllowDarkModeForWindow(control.Handle, true);
            }
        }

        private static class NativeMethods
        {
            public const int APPMODE_ALLOWDARK = 1;
            public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

            [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#132")]
            public static extern bool ShouldAppsUseDarkMode();

            [DllImport("uxtheme.dll", EntryPoint = "#133")]
            public static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

            [DllImport("uxtheme.dll", EntryPoint = "#135")]
            public static extern int SetPreferredAppMode(int appMode);

            [DllImport("DwmApi")]
            public static extern HRESULT DwmSetWindowAttribute(
                IntPtr hwnd, 
                int attr, 
                ref int attrValue, 
                int attrSize);
        }
    }
}
