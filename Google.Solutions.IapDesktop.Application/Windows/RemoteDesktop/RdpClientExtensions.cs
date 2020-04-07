using MSTSCLib;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.RemoteDesktop
{
    internal static class RdpClientExtensions
    {
        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        internal unsafe static void SendKeys(
            this IMsRdpClientNonScriptable5 nonScriptable,
            params Keys[] keyCodes)
        {
            if (keyCodes.Length > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(keyCodes));
            }

            short[] keyUp = new short[keyCodes.Length * 2];
            int[] keyData = new int[keyCodes.Length * 2];

            for (int i = 0; i < keyCodes.Length; i++)
            {
                var virtualKeyCode = (int)MapVirtualKey((uint)keyCodes[i], 0);

                // Generate DOWN key presses.
                keyUp[i] = 0;
                keyData[i] = virtualKeyCode;

                // Generate UP key presses (in reverse order).
                keyUp[keyUp.Length - 1 - i] = 1;
                keyData[keyData.Length - 1 - i] = virtualKeyCode;
            }

            fixed (short* keyUpPtr = keyUp)
            fixed (int* keyDataPtr = keyData)
            {
                nonScriptable.SendKeysUnsafe(keyData.Length, (bool*)keyUpPtr, keyDataPtr);
            }
        }

        private unsafe static void SendKeysUnsafe(
            this IMsRdpClientNonScriptable5 nonScriptable,
            int keyDataLength,
            bool* keyUpPtr,
            int* keyDataPtr
            )
        {
            // There is something about wrapping this key in a special method.
            // Without the wrapper method, marshaling does not work properly.
            nonScriptable.SendKeys(keyDataLength, ref *keyUpPtr, ref *keyDataPtr);
        }
    }
}
