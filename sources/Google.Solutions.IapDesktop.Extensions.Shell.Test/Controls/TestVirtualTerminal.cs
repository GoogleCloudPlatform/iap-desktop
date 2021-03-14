//
// Copyright 2020 Google LLC
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

using Google.Solutions.IapDesktop.Application.Test;
using Google.Solutions.IapDesktop.Extensions.Shell.Controls;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVirtualTerminal : ApplicationFixtureBase
    {
        private readonly string Esc = "\u001b";

        private VirtualTerminal terminal;
        private Form form;
        private StringBuilder sendData;

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
            this.sendData = new StringBuilder();
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
                this.sendData.Append(args.Data);
            };

            form.Show();
            PumpWindowMessages();

            Clipboard.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            //for (int i = 0; i < 10; i++)
            //{
            //    PumpWindowMessages();
            //    Thread.Sleep(100);
            //}

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
        // Clipboard
        //---------------------------------------------------------------------

        [Test]
        public void WhenPastingClipboardContentWithCrlf_ThenTerminalSendsClipboardContentWithNewline()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.PasteClipboard();

            Assert.AreEqual("sample\ntext", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Clipboard: Ctrl+V
        //---------------------------------------------------------------------

        [Test]
        public void WhenCtrlVIsEnabled_ThenTypingCtrlVSendsClipboardContent()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.EnableCtrlV = true;
            this.terminal.SimulateKey(Keys.Control | Keys.V);

            Assert.AreEqual("sample\ntext", this.sendData.ToString());
        }

        [Test]
        public void WhenCtrlVIsDisabled_ThenTypingCtrlVSendsKeystroke()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.EnableCtrlV = false;
            this.terminal.SimulateKey(Keys.Control | Keys.V);

            Assert.AreEqual("\u0016", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Clipboard: Shift+Insert
        //---------------------------------------------------------------------

        [Test]
        public void WhenShiftInsertIsEnabled_ThenTypingShiftInsertSendsClipboardContent()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.EnableShiftInsert = true;
            this.terminal.SimulateKey(Keys.Shift | Keys.Insert);

            Assert.AreEqual("sample\ntext", this.sendData.ToString());
        }

        [Test]
        public void WhenShiftInsertIsDisabled_ThenTypingShiftInsertSendsKeystroke()
        {
            Clipboard.SetText("sample\r\ntext");

            this.terminal.EnableShiftInsert = false;
            this.terminal.SimulateKey(Keys.Shift | Keys.Insert);

            Assert.AreEqual("", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Clipboard: Ctrl+C
        //---------------------------------------------------------------------

        [Test]
        public void WhenCtrlCIsEnabledAndTextSelected_ThenTypingCtrlCSetsClipboardContentAndClearsSelection()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.SelectText(2, 0, 7, 1, TextSelectionDirection.Forward);

            this.terminal.EnableCtrlC = true;
            this.terminal.SimulateKey(Keys.Control | Keys.C);

            Assert.AreEqual("rst line\nsecond l", Clipboard.GetText());
            Assert.AreEqual(string.Empty, this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlCIsEnabledButNoTextSelected_ThenTypingCtrlCSendsKeystroke()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.ClearTextSelection();

            this.terminal.EnableCtrlC = true;
            this.terminal.SimulateKey(Keys.Control | Keys.C);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("\u0003", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlCIsDisabledAndTextSelected_ThenTypingCtrlCClearsSelection()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.SelectText(2, 0, 1, 7, TextSelectionDirection.Forward);

            this.terminal.EnableCtrlC = false;
            this.terminal.SimulateKey(Keys.Control | Keys.C);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlCIsDisabledAndNoTextSelected_ThenTypingCtrlCSendsKeystroke()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.ClearTextSelection();

            this.terminal.EnableCtrlC = false;
            this.terminal.SimulateKey(Keys.Control | Keys.C);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("\u0003", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Clipboard: Ctrl+Insert
        //---------------------------------------------------------------------

        [Test]
        public void WhenCtrlInsertIsEnabledAndTextSelected_ThenTypingCtrlInsertSetsClipboardContentAndClearsSelection()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.SelectText(2, 0, 7, 1, TextSelectionDirection.Forward);

            this.terminal.EnableCtrlInsert = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Insert);

            Assert.AreEqual("rst line\nsecond l", Clipboard.GetText());
            Assert.AreEqual(string.Empty, this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlInsertIsEnabledButNoTextSelected_ThenTypingCtrlInsertSendsKeystroke()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.ClearTextSelection();

            this.terminal.EnableCtrlInsert = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Insert);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlInsertIsDisabledAndTextSelected_ThenTypingCtrlInsertClearsSelection()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.SelectText(2, 0, 1, 7, TextSelectionDirection.Forward);

            this.terminal.EnableCtrlInsert = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Insert);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenCtrlInsertIsDisabledAndNoTextSelected_ThenTypingCtrlInsertSendsKeystroke()
        {
            this.terminal.ReceiveData(
                "first line\r\n" +
                "second line\r\n" +
                "third line");
            this.terminal.ClearTextSelection();

            this.terminal.EnableCtrlInsert = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Insert);

            Assert.AreEqual("", Clipboard.GetText());
            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Text selection.
        //---------------------------------------------------------------------

        private static string GenerateText(int rows, int columns)
        {
            Debug.Assert(columns > 10);
            var buffer = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                buffer.Append($"{i:D8}: ");
                for (int j = 0; j < columns - 10; j++)
                {
                    buffer.Append('.');
                }

                buffer.Append("\r\n");
            }

            return buffer.ToString().Trim();
        }

        [Test]
        public void WhenCtrlAEnabled_ThenTypingCtrlASelectsAllText()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.EnableCtrlA = true;
            this.terminal.SimulateKey(Keys.Control | Keys.A);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual(
                textStraddlingViewPort.Replace("\r\n", "\n"), 
                this.terminal.TextSelection);
        }

        [Test]
        public void WhenCtrlADisbled_ThenTypingCtrlASendsKeystroke()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.EnableCtrlA = false;
            this.terminal.SimulateKey(Keys.Control | Keys.A);

            Assert.AreEqual("\u0001", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenTextSelected_ThenTypingClearsSelectionAndSendsKey()
        {
            var textStraddlingViewPort = GenerateText(3, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.SelectText(0, 0, 20, 2, TextSelectionDirection.Forward);
            Assert.IsTrue(this.terminal.IsTextSelected);

            this.terminal.SimulateKey(Keys.Space);

            Assert.AreEqual(" ", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenTextSelected_ThenTypingEnterClearsSelectionWithoutSendingKey()
        {
            var textStraddlingViewPort = GenerateText(3, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.SelectText(0, 0, 20, 2, TextSelectionDirection.Forward);
            Assert.IsTrue(this.terminal.IsTextSelected);

            this.terminal.SimulateKey(Keys.Enter);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Modifiers.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypingUpArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Up);

            Assert.AreEqual($"{Esc}[A", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingDownArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Down);

            Assert.AreEqual($"{Esc}[B", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingRightArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Right);

            Assert.AreEqual($"{Esc}[C", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingLeftArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Left);

            Assert.AreEqual($"{Esc}[D", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingHome_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Home);

            Assert.AreEqual($"{Esc}[1~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingEnd_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.End);

            Assert.AreEqual($"{Esc}[4~", this.sendData.ToString());
        }


        //---------------------------------------------------------------------
        // Numpad & Function Keys.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypingAltV_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Alt| Keys.V);

            Assert.AreEqual($"{Esc}v", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingBackspace_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Back);

            Assert.AreEqual("\u007f", this.sendData.ToString());
        }

        [Test]
        [Ignore("Not supported by vtnetcore")]
        public void WhenTypingPause_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Pause);

            Assert.AreEqual("\u001a", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingEscape_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Escape);

            Assert.AreEqual(Esc + Esc, this.sendData.ToString());
        }

        [Test]
        public void WhenTypingInsert_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Insert);

            Assert.AreEqual($"{Esc}[2~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingDelete_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Delete);

            Assert.AreEqual($"{Esc}[3~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingPageUp_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.PageUp);

            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingPrior_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Prior);

            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingPageDown_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.PageDown);

            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingNext_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Next);

            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingFunctionKeyInLowerRange_ThenKeystrokeIsSent(
            [Range(1, 5)] int functionKey
            )
        {
            this.terminal.SimulateKey((Keys)(Keys.F1 + functionKey - 1));

            Assert.AreEqual($"{Esc}[{10 + functionKey}~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingFunctionKeyInMiddleRange_ThenKeystrokeIsSent(
            [Range(6, 10)] int functionKey
            )
        {
            this.terminal.SimulateKey((Keys)(Keys.F1 + functionKey - 1));

            Assert.AreEqual($"{Esc}[{11 + functionKey}~", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingFunctionKeyInUpperRange_ThenKeystrokeIsSent(
            [Range(11, 12)] int functionKey
            )
        {
            this.terminal.SimulateKey((Keys)(Keys.F1 + functionKey - 1));

            Assert.AreEqual($"{Esc}[{12 + functionKey}~", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Modifiers.
        //---------------------------------------------------------------------

        [Test]
        [Ignore("Not supported by vtnetcore")]
        public void WhenTypingCtrlSpace_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Control | Keys.Space);

            Assert.AreEqual("\u0000", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingCtrlUpArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Control | Keys.Up);

            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingCtrlDownArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Control | Keys.Down);

            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingCtrlRightArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Control | Keys.Right);

            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingCtrlLeftArrow_ThenKeystrokeIsSent()
        {
            this.terminal.SimulateKey(Keys.Control | Keys.Left);

            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
        }
    }
}
