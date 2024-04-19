using System;
using System.Windows.Forms;
using Google.Solutions.Platform.Dispatch;
using System.Diagnostics;
using Google.Solutions.Terminal.Controls;
using System.Drawing;

namespace Google.Solutions.Terminal.TestApp
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] argv)
        {
            using (var f = new Form()
            {
                Width = 800,
                Height = 600,
            })
            {
                var control = new VirtualTerminal()
                {
                    Dock = DockStyle.Fill,
                    ForeColor = Color.Beige,
                    BackColor = Color.DarkGray
                };

                f.Controls.Add(control);


                control.DeviceClosed += (_, __) => f.Close();
                control.DeviceError += (_, args) => MessageBox.Show(f, args.Exception.Message);

                IWin32Process? process = null;

                f.Shown += (_, __) =>
                {
                    //
                    // NB. We must initialize the pseudo-terminal with
                    // the right dimensions. Now that the window has been
                    // shown, we know these.
                    //
                    Debug.Assert(control.Dimensions.Width > 0);
                    Debug.Assert(control.Dimensions.Height > 0);

                    var processFactory = new Win32ProcessFactory();
                    process = processFactory.CreateProcessWithPseudoConsole(
                        "powershell.exe",
                        null,
                        control.Dimensions);

                    process.PseudoConsole!.OutputAvailable += (_, args) =>
                    {
                        Debug.WriteLine($"PTY: {args.Data}");
                    };

                    control.Device = process.PseudoConsole;

                    //TODO: close pty, process?

                    process.Resume();
                };

                control.Output += (_, args) =>
                {
                    Debug.WriteLine("Out: " + args.Data);
                };
                control.UserInput += (_, args) =>
                {
                    Debug.WriteLine("In: " + args.Data);
                };


                Application.Run(f);
            }
        }
    }
}