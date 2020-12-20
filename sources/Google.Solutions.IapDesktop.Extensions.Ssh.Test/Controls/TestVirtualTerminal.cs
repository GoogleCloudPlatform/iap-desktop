using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Ssh.Controls;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVirtualTerminal : ApplicationFixtureBase
    {
        private static void PumpMessages()
            => System.Windows.Forms.Application.DoEvents();

        // TODO: Cleanup

        //private void SendKey(IWin32Window window, char c)
        //{
        //    UnsafeNativeMethods.PostMessage(
        //        window.Handle,
        //        UnsafeNativeMethods.WM_CHAR,
        //        c,
        //        0);
        //    PumpMessages();
        //}

        //private enum SpecialKey : uint
        //{
        //    VK_BACK = 0x8,
        //    VK_RETURN = 0xD
        //}

        //private void SendKey(IWin32Window window, SpecialKey c)
        //{
        //    var virtualKey = UnsafeNativeMethods.MapVirtualKey((uint)c, 0);
        //    UnsafeNativeMethods.PostMessage(
        //        window.Handle,
        //        UnsafeNativeMethods.WM_KEYDOWN,
        //        (uint)c,
        //        0x0001 | virtualKey >> 16);
        //    UnsafeNativeMethods.PostMessage(
        //        window.Handle,
        //        UnsafeNativeMethods.WM_KEYUP,
        //        (uint)c,
        //        0x0001 | virtualKey >> 16 | 0xC0 >> 24);
        //    PumpMessages();
        //}

        //[Test]
        //public void WhenSendingBackspace_ThenLastCharacterIsErased()
        //{
        //    var terminal = new VirtualTerminal()
        //    {
        //        Dock = DockStyle.Fill
        //    };

        //    var form = new Form();
        //    form.Controls.Add(terminal);
        //    form.Show();
        //    PumpMessages();

        //    form.SendKeySequence(
        //        new[]
        //        {
        //            UnsafeNativeMethods.VirtualKeyShort.KEY_A,
        //            UnsafeNativeMethods.VirtualKeyShort.KEY_B,
        //            UnsafeNativeMethods.VirtualKeyShort.KEY_C,
        //            UnsafeNativeMethods.VirtualKeyShort.BACK,
        //        });

        //    terminal.Invalidate();

        //    PumpMessages();
        //    Assert.AreEqual("ab", terminal.GetBuffer().Trim());

        //    form.Close();
        //}

        //[Test]
        //public void WhenSendingBackspace_ThenLastCharacterIsErased()
        //{
        //    var terminal = new Terminal()
        //    {
        //        Dock = DockStyle.Fill
        //    };

        //    var form = new Form();
        //    form.Controls.Add(terminal);
        //    form.Show();
        //    PumpMessages();
        //    PumpMessages();
        //    PumpMessages();
        //    UnsafeNativeMethods.SetForegroundWindow(form.Handle);
        //    PumpMessages();

        //    SendKeys(new[]
        //    {
        //        UnsafeNativeMethods.ScanCodeShort.KEY_A,
        //        UnsafeNativeMethods.ScanCodeShort.KEY_B,
        //        UnsafeNativeMethods.ScanCodeShort.KEY_C,
        //        UnsafeNativeMethods.ScanCodeShort.RETURN
        //    });


        //    terminal.Invalidate();

        //    PumpMessages();
        //    Assert.AreEqual("ab", terminal.GetBuffer().Trim());

        //    form.Close();
        //}

        //private static void SendKeys(
        //    UnsafeNativeMethods.ScanCodeShort[] scanCodes)
        //{
        //    var inputs = new UnsafeNativeMethods.INPUT[scanCodes.Length];
        //    for (int i = 0; i < scanCodes.Length; i++)
        //    {
        //        inputs[i] = new UnsafeNativeMethods.INPUT();
        //        inputs[i].type = 1; // Keyboard.
        //        inputs[i].U.ki.wScan = scanCodes[i];
        //        inputs[i].U.ki.dwFlags = UnsafeNativeMethods.KEYEVENTF.SCANCODE;
        //    }

        //    UnsafeNativeMethods.SendInput(
        //        (uint)inputs.Length,
        //        inputs,
        //        UnsafeNativeMethods.INPUT.Size);
        //}
        
        //[Test]
        //public void __test()
        //{

        //    var terminal = new Terminal()
        //    {
        //        Dock = DockStyle.Fill
        //    };

        //    var form = new Form()
        //    {
        //        Size = new System.Drawing.Size(500, 300)
        //    };
        //    form.Controls.Add(terminal);

        //    terminal.PushText("test");
        //    System.Windows.Forms.Application.Run(form);
        //    //form.ShowDialog();
        //}

        //private UnsafeNativeMethods.VirtualKeyShort VirtualKeyCodeFronScanCode(
        //    UnsafeNativeMethods.ScanCodeShort sc)
        //{
        //    return (UnsafeNativeMethods.VirtualKeyShort)UnsafeNativeMethods.MapVirtualKey(
        //        (uint)sc, 
        //        UnsafeNativeMethods.MAPVK_VSC_TO_VK);
        //}

        //private UnsafeNativeMethods.ScanCodeShort ScanCodeFromVirtualKeyCode(
        //    UnsafeNativeMethods.VirtualKeyShort vk)
        //{
        //    return (UnsafeNativeMethods.ScanCodeShort)UnsafeNativeMethods.MapVirtualKey(
        //        (uint)vk,
        //        UnsafeNativeMethods.MAPVK_VK_TO_VSC);
        //}

        //[Test]
        //public void __test()
        //{
        //    var sc = ScanCodeFromVirtualKeyCode((UnsafeNativeMethods.VirtualKeyShort)'A');
        //    var vk = VirtualKeyCodeFronScanCode(sc);
        //    Assert.AreEqual((uint)vk, (uint)'A');
        //}
    }
}
