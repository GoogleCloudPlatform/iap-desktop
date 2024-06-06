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

            public TerminalForm()
            {
                this.VirtualTerminal = new VirtualTerminal()
                {
                    Dock = DockStyle.Fill
                };

                this.Controls.Add(this.VirtualTerminal);
            }
        }

        //---------------------------------------------------------------------
        // Resizing.
        //---------------------------------------------------------------------

        [Test]
        public void WhenControlResized_ThenDimensionsAreUpdated()
        {
            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
                form.Show();

                var initialDimensions = form.VirtualTerminal.Dimensions;

                Assert.AreNotEqual(0, initialDimensions.Width);
                Assert.AreNotEqual(0, initialDimensions.Height);

                form.Size = new Size(800, 800);
                Assert.Greater(form.VirtualTerminal.Dimensions.Width, initialDimensions.Width);
                Assert.Greater(form.VirtualTerminal.Dimensions.Height, initialDimensions.Height);

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // Send.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCharKeyPressed_ThenTerminalSendsData()
        {
            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
                form.Show();

                VirtualTerminalAssert.RaisesSendEvent(
                    form.VirtualTerminal,
                    "A",
                    () => form.VirtualTerminal.SimulateKey(Keys.A));

                form.Close();
            }
        }

        [Test]
        public void WhenEnterKeyPressed_ThenTerminalSendsData()
        {
            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
                form.Show();

                VirtualTerminalAssert.RaisesSendEvent(
                    form.VirtualTerminal,
                    "\r",
                    () => form.VirtualTerminal.SimulateKey(Keys.Enter));

                form.Close();
            }
        }

        [Test]
        public void WhenLeftKeyPressed_ThenTerminalSendsData()
        {
            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
                form.Show();

                VirtualTerminalAssert.RaisesSendEvent(
                    form.VirtualTerminal,
                    "\u001b[D\u001b[D",
                    () => form.VirtualTerminal.SimulateKey(Keys.Left));

                form.Close();
            }
        }

        //---------------------------------------------------------------------
        // Device.
        //---------------------------------------------------------------------

        [Test]
        public void WhenDeviceChanged_ThenScreenIsCleared()
        {
            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
                form.VirtualTerminal.Device = new Mock<IPseudoConsole>().Object;
                form.Show();

                string receivedData = string.Empty;
                form.VirtualTerminal.Output += (_, args) => receivedData += args.Data;

                // Change device.
                form.VirtualTerminal.Device = new Mock<IPseudoConsole>().Object;

                form.Close();

                StringAssert.Contains("\u001b[2J", receivedData);
            }
        }

        [Test]
        public void WhenDeviceChanged_ThenPreviousBindingIsDisposedAndScreenIsCleared()
        {
            using (var form = new TerminalForm())
            {
                var device = new Mock<IPseudoConsole>();

                form.Size = new Size(500, 500);
                form.VirtualTerminal.Device = device.Object;
                form.Show();

                // Change device.
                form.VirtualTerminal.Device = new Mock<IPseudoConsole>().Object;

                form.Close();

                device.Verify(d => d.Dispose(), Times.Once);
            }
        }

        [Test]
        public void WhenDisposed_ThenDeviceIsDisposed()
        {
            var device = new Mock<IPseudoConsole>();

            using (var form = new TerminalForm())
            {
                form.Size = new Size(500, 500);
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
        public void WhenForeColorChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.ForeColor = Color.Yellow;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }

        [Test]
        public void WhenBackColorChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.BackColor = Color.Yellow;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }

        [Test]
        public void WhenFontChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.Font = SystemFonts.DialogFont;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }

        [Test]
        public void WhenSelectionBackColorChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.SelectionBackColor = Color.AliceBlue;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }

        [Test]
        public void WhenSelectionBackgroundAlphaChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.SelectionBackgroundAlpha = .99f;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }

        [Test]
        public void WhenCaretStyleChanged_ThenThemeIsChanged()
        {
            using (var form = new TerminalForm())
            {
                form.Show();

                var themeChangeEventRaised = false;
                form.VirtualTerminal.ThemeChanged += (_, __) => themeChangeEventRaised = true;

                form.VirtualTerminal.Caret = VirtualTerminal.CaretStyle.BlinkingUnderline;
                form.Close();

                Assert.IsTrue(themeChangeEventRaised);
            }
        }
    }

    public static class VirtualTerminalAssert
    {
        public static void RaisesSendEvent(
            VirtualTerminal terminal,
            string expectedData,
            Action action)
        {
            var receiveBuffer = new StringBuilder();
            void AccumulateData(object sender, TerminalInputEventArgs args)
            {
                receiveBuffer.Append(args.Data);
            }

            terminal.UserInput += AccumulateData;
            action();
            terminal.UserInput -= AccumulateData;

            Assert.AreEqual(receiveBuffer.ToString(), expectedData);
        }
    }
}

