using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Shell
{
    /// <summary>
    /// Wrapper class for Windows 10 Virtual Desktop Manager.
    /// </summary>
    public static class VirtualDesktopExtensions
    {
        private const uint TYPE_E_ELEMENTNOTFOUND = unchecked(0x8002802B);

        /// <summary>
        /// Check if the window is on the current virtual desktop.
        public static bool IsOnCurrentVirtualDesktop(this IWin32Window window)
        {
            window.ExpectNotNull(nameof(window));

            using (var manager = ComReference.For(
                (IVirtualDesktopManager)new VirtualDesktopManager()))
            {
                return manager.Object.IsWindowOnCurrentVirtualDesktop(window.Handle) != 0;
            }
        }

        /// <summary>
        /// Get the ID of the virtual desktop containing a window.
        /// </summary>
        public static Guid GetVirtualDesktopId(this IWin32Window window)
        {
            window.ExpectNotNull(nameof(window));

            using (var manager = ComReference.For(
                (IVirtualDesktopManager)new VirtualDesktopManager()))
            {
                try
                {
                    return manager.Object.GetWindowDesktopId(window.Handle);
                }
                catch (COMException e) when ((uint)e.HResult == TYPE_E_ELEMENTNOTFOUND)
                {
                    throw new ArgumentException("Window not found");
                }
            }
        }

        /// <summary>
        /// Move the window to a different desktop.
        /// </summary>
        public static void MoveToVirtualDesktop(
            this IWin32Window window,
            Guid desktopId)
        {
            window.ExpectNotNull(nameof(window));

            using (var manager = ComReference.For(
                (IVirtualDesktopManager)new VirtualDesktopManager()))
            {
                try
                {
                    manager.Object.MoveWindowToDesktop(window.Handle, desktopId);
                }
                catch (COMException e) when ((uint)e.HResult == TYPE_E_ELEMENTNOTFOUND)
                {
                    throw new ArgumentException("Desktop not found");
                }
            }
        }

        //---------------------------------------------------------------------
        // Interop.
        //---------------------------------------------------------------------

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
        private interface IVirtualDesktopManager
        {
            int IsWindowOnCurrentVirtualDesktop(
                [In] IntPtr hwnd);

            Guid GetWindowDesktopId(
                [In] IntPtr hwnd);

            int MoveWindowToDesktop(
                [In] IntPtr hwnd,
                [MarshalAs(UnmanagedType.LPStruct)]
                [In] Guid desktop);
        }

        [ComImport]
        [Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
        [ClassInterface(ClassInterfaceType.None)]
        private class VirtualDesktopManager
        {
        }
    }
}