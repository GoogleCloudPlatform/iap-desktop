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

        private static bool IsModifierKey(Keys key)
        {
            switch (key)
            {
                case Keys.ShiftKey:
                case Keys.ControlKey:
                case Keys.Menu:
                case Keys.LWin:
                case Keys.RWin:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    return true;
                default:
                    return false;
            }
        }

        private enum VariantBool : short
        {
            True = -1,
            False = 0
        }

        private struct SendKeysData
        {
            public const int MaxKeys = 20;

            public unsafe fixed short keyUp[20];

            public unsafe fixed int keyData[20];
        }

        //internal unsafe static void SendKeys(
        //    this IMsRdpClientNonScriptable5 nonScriptable,
        //    params Keys[] keyCodes)
        //{
        //    SendKeysData sendKeysData = default(SendKeysData);
        //    bool* ptr = (bool*)sendKeysData.keyUp;
        //    int* ptr2 = sendKeysData.keyData;
        //    int num2 = 0;
        //    for (int i = 0; i < keyCodes.Length && i < 10; i++)
        //    {
        //        int num3 = (int)MapVirtualKey((uint)keyCodes[i], 0u);
        //        sendKeysData.keyData[num2] = num3;
        //        sendKeysData.keyUp[num2++] = 0;
        //        if (!IsModifierKey(keyCodes[i]))
        //        {
        //            for (int num5 = num2 - 1; num5 >= 0; num5--)
        //            {
        //                sendKeysData.keyData[num2] = sendKeysData.keyData[num5];
        //                sendKeysData.keyUp[num2++] = 1;
        //            }
        //            nonScriptable.SendKeys(num2, ref *ptr, ref *ptr2);
        //            num2 = 0;
        //        }
        //    }
        //}

        //internal unsafe static void SendKeys(
        //    this IMsRdpClientNonScriptable5 nonScriptable,
        //    params Keys[] keyCodes)
        //{
        //    SendKeysData sendKeysData = default(SendKeysData);
        //    bool* keyUpPtr = (bool*)sendKeysData.keyUp;
        //    int* keyDataPtr = sendKeysData.keyData;

        //    for (int i = 0; i < keyCodes.Length; i++)
        //    {
        //        int virtualKeyCode = (int)MapVirtualKey((uint)keyCodes[i], 0u);

        //        // Generate DOWN key presses.
        //        sendKeysData.keyUp[i] = 0;
        //        sendKeysData.keyData[i] = virtualKeyCode;

        //        // Generate UP key presses (in reverse order).
        //        sendKeysData.keyUp[keyCodes.Length * 2 - 1 - i] = 1;
        //        sendKeysData.keyData[keyCodes.Length * 2 - 1 - i] = virtualKeyCode;
        //    }

        //    // This call is magic.
        //    IsModifierKey(keyCodes[0]);

        //    nonScriptable.SendKeys(keyCodes.Length * 2, ref *keyUpPtr, ref *keyDataPtr);
        //}
        ///
        //internal unsafe static void SendKeys(
        //    this IMsRdpClientNonScriptable5 nonScriptable,
        //    params Keys[] keyCodes)
        //{
        //    if (keyCodes.Length > 10)
        //    {
        //        throw new ArgumentOutOfRangeException(nameof(keyCodes));
        //    }

        //    var sendKeysData = default(SendKeysData);
        //    bool* keyUpPtr = (bool*)sendKeysData.keyUp;
        //    int* keyDataPtr = sendKeysData.keyData;

        //    for (int i = 0; i < keyCodes.Length; i++)
        //    {
        //        var virtualKeyCode = (int)MapVirtualKey((uint)keyCodes[i], 0);

        //        // Generate DOWN key presses.
        //        sendKeysData.keyUp[i] = 0;
        //        sendKeysData.keyData[i] = virtualKeyCode;

        //        // Generate UP key presses (in reverse order).
        //        sendKeysData.keyUp[keyCodes.Length * 2 - 1 - i] = 1;
        //        sendKeysData.keyData[keyCodes.Length * 2 - 1 - i] = virtualKeyCode;
        //    }

        //    nonScriptable.SendKeys(keyCodes.Length * 2, ref *keyUpPtr, ref *keyDataPtr);
        //}

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
