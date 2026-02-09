//
// Copyright 2024 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Platform.IO;
using Google.Solutions.Terminal.Controls;
using Moq;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal class TestVirtualTerminal
    {
        private class TerminalForm : Form
        {
            public VirtualTerminal VirtualTerminal { get; }

            private TerminalForm()
            {
                this.VirtualTerminal = new VirtualTerminal()
                {
                    Dock = DockStyle.Fill
                };

                this.Controls.Add(this.VirtualTerminal);
            }

            internal static TerminalForm Create()
            {
                var form = new TerminalForm()
                {
                    Size = new Size(800, 600)
                };

                //
                // When run in a headless environment, the terminal
                // might not be initialized automatically, so force
                // initialization if necessary.
                //
                if (!form.VirtualTerminal.TerminalHandleCreated)
                {
                    form.VirtualTerminal.CreateTerminalHandle();
                }

                return form;
            }
        }

        //---------------------------------------------------------------------
        // Resizing.
        //---------------------------------------------------------------------

        [Test]
        public void Size_WhenChanged_ThenUpdatesDimensions()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var initialDimensions = form.VirtualTerminal.Dimensions;

                Assert.That(initialDimensions.Width, Is.Not.EqualTo(0));
                Assert.That(initialDimensions.Height, Is.Not.EqualTo(0));

                form.Size = new Size(form.Size.Width + 100, form.Size.Height + 100);
                Application.DoEvents();

                Assert.That(form.VirtualTerminal.Dimensions.Width, Is.GreaterThan(initialDimensions.Width));
                Assert.That(form.VirtualTerminal.Dimensions.Height, Is.GreaterThan(initialDimensions.Height));

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // SimulateKey
        //---------------------------------------------------------------------

        [Test]
        public void SimulateKey_CharKey()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                VirtualTerminalAssert.RaisesUserInputEvent(
                    form.VirtualTerminal,
                    "a",
                    () => form.VirtualTerminal.SimulateKey(Keys.A));

                form.Close();
            }
        }

        [Test]
        public void SimulateKey_EnterKey()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                VirtualTerminalAssert.RaisesUserInputEvent(
                    form.VirtualTerminal,
                    "\r",
                    () => form.VirtualTerminal.SimulateKey(Keys.Enter));

                form.Close();
            }
        }

        [Test]
        public void SimulateKey_LeftKey()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                VirtualTerminalAssert.RaisesUserInputEvent(
                    form.VirtualTerminal,
                    "\u001b[D",
                    () => form.VirtualTerminal.SimulateKey(Keys.Left));

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // Device.
        //---------------------------------------------------------------------

        [Test]
        public void Device_WhenChanged_ThenClearsScreen()
        {
            using (var form = TerminalForm.Create())
            {
                form.VirtualTerminal.Device = new Mock<IPseudoTerminal>().Object;
                form.Show();

                var receivedData = string.Empty;
                form.VirtualTerminal.Output += (_, args) => receivedData += args.Data;

                // Change device.
                form.VirtualTerminal.Device = new Mock<IPseudoTerminal>().Object;

                form.Close();

                Assert.That(receivedData, Does.Contain("\u001b[2J"));
            }
        }

        [Test]
        public void Device_WhenChanged_ThenDisposesPreviousBindingAndClearsScreen()
        {
            using (var form = TerminalForm.Create())
            {
                var device = new Mock<IPseudoTerminal>();

                form.VirtualTerminal.Device = device.Object;
                form.Show();

                // Change device.
                form.VirtualTerminal.Device = new Mock<IPseudoTerminal>().Object;

                form.Close();

                device.Verify(d => d.Dispose(), Times.Once);
            }
        }

        [Test]
        public void Device_WhenDisposed_ThenDisposesDevice()
        {
            var device = new Mock<IPseudoTerminal>();

            using (var form = TerminalForm.Create())
            {
                form.VirtualTerminal.Device = device.Object;
                form.Show();
                form.Close();
            }

            device.Verify(d => d.Dispose(), Times.Once);
        }

        //---------------------------------------------------------------------
        // Theme.
        //---------------------------------------------------------------------

        [Test]
        public void ForeColor_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.ForeColor = Color.Yellow;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        [Test]
        public void BackColor_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.BackColor = Color.Yellow;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        [Test]
        public void Font_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.Font = SystemFonts.DialogFont;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        [Test]
        public void SelectionBackColor_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.SelectionBackColor = Color.AliceBlue;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        [Test]
        public void SelectionBackgroundAlpha_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.SelectionBackgroundAlpha = .99f;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        [Test]
        public void CaretStyle_WhenChanged()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.Caret = VirtualTerminal.CaretStyle.BlinkingUnderline;
                form.Close();

                Assert.That(themeChangeEventRaised, Is.True);
            }
        }

        //---------------------------------------------------------------------
        // Dimensions.
        //---------------------------------------------------------------------

        [Test]
        public void Dimensions()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                Assert.That(form.VirtualTerminal.Dimensions.Width, Is.GreaterThan(80));
                Assert.That(form.VirtualTerminal.Dimensions.Height, Is.GreaterThan(20));

                form.Close();
            }
        }

        [Test]
        public void Dimensions_WhenMinimized_KeepsDimensions()
        {
            using (var form = TerminalForm.Create())
            {
                form.Show();

                var dimensions = form.VirtualTerminal.Dimensions;

                form.WindowState = FormWindowState.Minimized;
                Assert.That(form.VirtualTerminal.Dimensions, Is.EqualTo(dimensions));

                form.WindowState = FormWindowState.Normal;
                Assert.That(form.VirtualTerminal.Dimensions, Is.EqualTo(dimensions));

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // SanitizeTextForPasting.
        //---------------------------------------------------------------------

        [Test]
        public void SanitizeTextForPasting_TypographicQuoteConversion()
        {
            var terminal = new VirtualTerminal()
            {
                EnableBracketedPaste = false,
                EnableTypographicQuoteConversion = false,
            };

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BB\r\nand that"), Is.EqualTo("\u00ABThis\u00BB\rand that"));

            terminal.EnableTypographicQuoteConversion = true;

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BB\r\nand that"), Is.EqualTo("\"This\"\rand that"));
        }

        [Test]
        public void SanitizeTextForPasting_SanitizeWhitespace()
        {
            var terminal = new VirtualTerminal();

            Assert.That(
                terminal.SanitizeTextForPasting("\t\r\n  one\t\r\ntwo \t\r\n "), Is.EqualTo("\t\r  one\t\rtwo"));
        }

        [Test]
        public void SanitizeTextForPasting_BracketedPasting_SingleLine()
        {
            var terminal = new VirtualTerminal()
            {
                EnableBracketedPaste = false,
                EnableTypographicQuoteConversion = false,
            };

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BBand that\n"), Is.EqualTo("\u00ABThis\u00BBand that"));

            terminal.EnableBracketedPaste = true;

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BBand that\n"), Is.EqualTo("\u00ABThis\u00BBand that"));
        }

        [Test]
        public void SanitizeTextForPasting_BracketedPasting_MultiLine()
        {
            var terminal = new VirtualTerminal()
            {
                EnableBracketedPaste = false,
                EnableTypographicQuoteConversion = false,
            };

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BB\r\nand that\n"), Is.EqualTo("\u00ABThis\u00BB\rand that"));

            terminal.EnableBracketedPaste = true;

            Assert.That(
                terminal.SanitizeTextForPasting("\u00ABThis\u00BB\r\nand that\n"), Is.EqualTo("\u001b[200~\u00ABThis\u00BB\rand that\u001b[201~"));
        }
    }

    public static class VirtualTerminalAssert
    {
        public static void RaisesUserInputEvent(
            VirtualTerminal terminal,
            string expectedData,
            Action action)
        {
            var receiveBuffer = new StringBuilder();
            void AccumulateData(object sender, VirtualTerminalInputEventArgs args)
            {
                receiveBuffer.Append(args.Data);
            }

            terminal.UserInput += AccumulateData;
            action();
            terminal.UserInput -= AccumulateData;

            Assert.That(receiveBuffer.ToString(), Is.EqualTo(expectedData));
        }
    }
}

