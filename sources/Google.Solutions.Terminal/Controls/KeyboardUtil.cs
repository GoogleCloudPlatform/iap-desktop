using Google.Solutions.Mvvm.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    internal static class KeyboardUtil
    {
        public static IEnumerable<Message> ToMessageSequence(
            IntPtr hwnd,
            Keys key)
        {
            var keyboardState = new byte[255];
            if (!NativeMethods.GetKeyboardState(keyboardState)) // TOOD: remove?
            {
                throw new InvalidOperationException();
            }

            var virtualKeyCode = (uint)key;
            var scanCode = NativeMethods.MapVirtualKey(virtualKeyCode, 0);

            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_KEYDOWN,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };


            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_CHAR,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };


            yield return new Message()
            {
                HWnd = hwnd,
                Msg = (int)WindowMessage.WM_KEYUP,
                LParam = new IntPtr((scanCode & 0xFF) << 16),
                WParam = new IntPtr(virtualKeyCode),
            };
        }

        //---------------------------------------------------------------------
        // P/Invoke definitions.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern bool GetKeyboardState(byte[] lpKeyState);

            [DllImport("user32.dll")]
            internal static extern uint MapVirtualKey(uint uCode, uint uMapType);
        }
    }
}
