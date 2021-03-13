using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Controls;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVirtualTerminal : ApplicationFixtureBase
    {
        private VirtualTerminal terminal;
        private Form form;
        private StringBuilder receivedInput;

        protected static void PumpWindowMessages()
            => System.Windows.Forms.Application.DoEvents();

        protected IList<string> GetOutput()
            => this.terminal.GetBuffer()
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

        [SetUp]
        public void SetUp()
        {
            this.receivedInput = new StringBuilder();
            this.form = new Form()
            {
                Size = new Size(800, 600)
            };

            terminal = new VirtualTerminal()
            {
                Dock = DockStyle.Fill
            };
            form.Controls.Add(terminal);

            terminal.SendData += (sender, args) =>
            {
                this.receivedInput.Append(args.Data);
            };

            form.Show();
            PumpWindowMessages();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < 50; i++)
            {
                PumpWindowMessages();
                Thread.Sleep(100);
            }

            this.form.Close();
        }

        [Test]
        public void WhenReceivedTextContainsCrlf_ThenOutputSpansTwoRows()
        {
            this.terminal.ReceiveData("sample\r\ntext");

            var output = GetOutput();
            Assert.AreEqual(2, output.Count);
            Assert.AreEqual("sample", output[0]);
            Assert.AreEqual("text", output[1]);
        }

        //---------------------------------------------------------------------
        // Clipboard.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPastingCrlf_ThenTerminalSendsNewline()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.PasteClipboard();

            Assert.AreEqual("sample\ntext", this.receivedInput.ToString());
        }

    }
}
