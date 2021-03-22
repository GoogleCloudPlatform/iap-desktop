﻿//
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
            //for (int i = 0; i < 20; i++)
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
        // Text selection: Ctrl+A
        //---------------------------------------------------------------------

        private static string GenerateText(int rows, int columns)
        {
            Debug.Assert(columns >= 10);
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
        public void WhenCtrlAEnabledAndCursorAtEnd_ThenTypingCtrlASelectsAllText()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.EnableCtrlA = true;
            this.terminal.SimulateKey(Keys.Control | Keys.A);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual(
                textStraddlingViewPort.Replace("\r\n", "\n") + "\n", 
                this.terminal.TextSelection);
        }

        [Test]
        public void WhenCtrlAEnabledAndCursorNotAtEnd_ThenTypingCtrlASelectsAllText()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.EnableCtrlA = true;
            this.terminal.MoveCursorRelative(-5, -1);
            this.terminal.SimulateKey(Keys.Control | Keys.A);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual(
                textStraddlingViewPort.Replace("\r\n", "\n") + "\n",
                this.terminal.TextSelection);
        }

        [Test]
        public void WhenCtrlAEnabledAndCursorNotAtEnd_ThenTypingCtrlACtrlCCopiesTimmedTextToClipboard()
        {
            var textSmallerThanViewPort = GenerateText(3, 20);
            this.terminal.ReceiveData(textSmallerThanViewPort);

            this.terminal.EnableCtrlA = true;
            this.terminal.EnableCtrlC = true;
            this.terminal.MoveCursorRelative(-5, -1);
            this.terminal.SimulateKey(Keys.Control | Keys.A);
            this.terminal.SimulateKey(Keys.Control | Keys.C);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
            Assert.AreEqual(
                textSmallerThanViewPort.Replace("\r\n", "\n"),
                Clipboard.GetText());
        }

        [Test]
        public void WhenCtrlAEnabledAndScrolledToTop_ThenTypingCtrlASelectsAllText()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.EnableCtrlA = true;
            this.terminal.ScrollViewPort(-100); // All the way up.
            this.terminal.SimulateKey(Keys.Control | Keys.A);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual(
                textStraddlingViewPort.Replace("\r\n", "\n") + "\n",
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

        //---------------------------------------------------------------------
        // Text selection: Shift+Left/Right
        //---------------------------------------------------------------------

        [Test]
        public void WhenShiftLeftRightEnabled_ThenTypingShiftLeftStartsSelection()
        {
            this.terminal.ReceiveData("0123456789");

            this.terminal.EnableShiftLeftRight = true;
            this.terminal.MoveCursorRelative(-5, 0);
            this.terminal.SimulateKey(Keys.Shift | Keys.Left);

            // Start single-character selection.
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual("45", this.terminal.TextSelection);

            // Extend selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Left, 2);
            Assert.AreEqual("2345", this.terminal.TextSelection);

            // Shrink selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Right, 2);
            Assert.AreEqual("45", this.terminal.TextSelection);

            // Invert selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Right, 2);
            Assert.AreEqual("56", this.terminal.TextSelection);

            Assert.AreEqual(string.Empty, this.sendData.ToString());
        }

        [Test]
        public void WhenShiftLeftRightEnabled_ThenTypingShiftRightStartsSelection()
        {
            this.terminal.ReceiveData("0123456789");

            this.terminal.EnableShiftLeftRight = true;
            this.terminal.MoveCursorRelative(-5, 0);
            this.terminal.SimulateKey(Keys.Shift | Keys.Right);

            // Start single-character selection.
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual("56", this.terminal.TextSelection);

            // Extend selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Right, 2);
            Assert.AreEqual("5678", this.terminal.TextSelection);

            // Shrink selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Left, 2);
            Assert.AreEqual("56", this.terminal.TextSelection);

            // Invert selection.
            this.terminal.SimulateKey(Keys.Shift | Keys.Left, 2);
            Assert.AreEqual("45", this.terminal.TextSelection);

            Assert.AreEqual(string.Empty, this.sendData.ToString());
        }

        [Test]
        public void WhenShiftLeftRightEnabled_ThenTypingShiftLeftOrRightExtendsSelectionBeyondCurrentRow()
        {
            this.terminal.ReceiveData(
                "abcde\r\n" +
                "0123456789\r\n" +
                "vwxyz");

            this.terminal.EnableShiftLeftRight = true;
            this.terminal.MoveCursorRelative(0, -1);
            this.terminal.SimulateKey(Keys.Shift | Keys.Left, 6);

            // Move left until preceding line.
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual("e\n012345", this.terminal.TextSelection);

            // Move left till end.
            this.terminal.SimulateKey(Keys.Shift | Keys.Left, 10);
            Assert.AreEqual("abcde\n012345", this.terminal.TextSelection);

            // Move right till third line.
            this.terminal.SimulateKey(Keys.Shift | Keys.Right, 16);
            Assert.AreEqual("56789\nvw", this.terminal.TextSelection);

            // Move right till end.
            this.terminal.SimulateKey(Keys.Shift | Keys.Right, 10);
            Assert.AreEqual("56789\nvwxyz\n\n\n\n\n\n\n", this.terminal.TextSelection);
            
            Assert.AreEqual(string.Empty, this.sendData.ToString());
        }

        [Test]
        public void WhenShiftLeftRightDisabled_ThenTypingShiftLeftSendsKeystroke()
        {
            this.terminal.EnableShiftLeftRight = false;
            this.terminal.SimulateKey(Keys.Shift | Keys.Left);

            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenShiftLeftRightDisabled_ThenTypingShiftRightSendsKeystroke()
        {
            this.terminal.EnableShiftLeftRight = false;
            this.terminal.SimulateKey(Keys.Shift | Keys.Right);

            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Text selection: Shift+Up/Down
        //---------------------------------------------------------------------

        [Test]
        public void WhenShiftUpDownEnabled_ThenTypingUpOrDownExtendsSelectionBeyondCurrentRow()
        {
            this.terminal.ReceiveData(
                "abcde\r\n" +
                "0123456789\r\n" +
                "vwxyz");

            this.terminal.EnableShiftLeftRight = true;
            this.terminal.MoveCursorRelative(-2, -1);
            this.terminal.SimulateKey(Keys.Shift | Keys.Up, 1);

            // Move Up to preceding line.
            Assert.IsTrue(this.terminal.IsTextSelected);
            Assert.AreEqual("de\n0123", this.terminal.TextSelection);

            // Move up beyond top.
            this.terminal.SimulateKey(Keys.Shift | Keys.Up, 2);
            Assert.AreEqual("de\n0123", this.terminal.TextSelection);

            // Move down till third line.
            this.terminal.SimulateKey(Keys.Shift | Keys.Down, 2);
            Assert.AreEqual("3456789\nvwxy", this.terminal.TextSelection);

            // Move down beyond end.
            this.terminal.SimulateKey(Keys.Shift | Keys.Down, 1);
            Assert.AreEqual("3456789\nvwxyz\n", this.terminal.TextSelection);

            // Move up again.
            this.terminal.SimulateKey(Keys.Shift | Keys.Up, 1);
            Assert.AreEqual("3456789\nvwxy", this.terminal.TextSelection);

            Assert.AreEqual(string.Empty, this.sendData.ToString());
        }

        [Test]
        public void WhenShiftUpDownDisabled_ThenTypingShiftDownSendsKeystroke()
        {
            this.terminal.EnableShiftUpDown = false;
            this.terminal.SimulateKey(Keys.Shift | Keys.Down);

            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenShiftUpDownDisabled_ThenTypingShiftUpSendsKeystroke()
        {
            this.terminal.EnableShiftUpDown = false;
            this.terminal.SimulateKey(Keys.Shift | Keys.Up);

            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Text selection: Clearing
        //---------------------------------------------------------------------

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
        // Navigation: Control+Left/Right
        //---------------------------------------------------------------------

        [Test]
        public void WhenControlLeftRightEnabled_ThenTypingControlLeftSendsJumpWordKeystroke()
        {
            this.terminal.EnableCtrlLeftRight = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Left);

            Assert.AreEqual($"{Esc}[1;5D", this.sendData.ToString());
        }

        [Test]
        public void WhenControlLeftRightDisabled_ThenTypingControlLeftSendsKeystroke()
        {
            this.terminal.EnableCtrlLeftRight = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Right);

            Assert.AreEqual($"{Esc}[1;5C", this.sendData.ToString());
        }

        [Test]
        public void WhenControlLeftRightDisabled_ThenTypingControlRightSendsKeystroke()
        {
            this.terminal.EnableCtrlLeftRight = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Right);

            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenControlLeftRightDisabled_ThenTypingControlKeftSendsKeystroke()
        {
            this.terminal.EnableCtrlLeftRight = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Left);

            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Scrolling: Control+Up/Down
        //---------------------------------------------------------------------

        [Test]
        public void WhenControlUpDownEnabledAndTerminalFull_ThenTypingControlUpScrollsUpOneLine()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            Assert.AreNotEqual(0, this.terminal.ViewTop);
            var previousViewTop = this.terminal.ViewTop;

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Up);

            Assert.AreEqual(previousViewTop - 1, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlUpDownEnabledAndTerminalNotFull_ThenTypingControlUpIsIgnored()
        {
            var textNotEnoughToFillViewPort = GenerateText(3, 20);
            this.terminal.ReceiveData(textNotEnoughToFillViewPort);

            Assert.AreEqual(0, this.terminal.ViewTop);

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Up);

            Assert.AreEqual(0, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlUpDownEnabledAndTerminalFull_ThenTypingControlDownScrollsUpOneLine()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            this.terminal.ScrollToTop();
            Assert.AreEqual(0, this.terminal.ViewTop);

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Down, 2);

            Assert.AreEqual(2, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlUpDownEnabledAndTerminalScrolledToEnd_ThenTypingControlDownIsIgnored()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            var previousViewTop = this.terminal.ViewTop;

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Down);

            Assert.AreEqual(previousViewTop, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlUpDownDisabled_ThenTypingControlUpSendsKeystroke()
        {
            this.terminal.EnableCtrlUpDown = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Up);

            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenControlUpDownDisabled_ThenTypingControlDownSendsKeystroke()
        {
            this.terminal.EnableCtrlUpDown = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Down);

            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        //---------------------------------------------------------------------
        // Scrolling: Control+Home/End
        //---------------------------------------------------------------------

        [Test]
        public void WhenControlHomeEndEnabledAndTerminalFull_ThenTypingControlHomeScrollsToTop()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            Assert.AreNotEqual(0, this.terminal.ViewTop);

            this.terminal.EnableCtrlHomeEnd = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Home);

            Assert.AreEqual(0, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlHomeEndEnabledAndTerminalNotFull_ThenTypingControlHomeIsIgnored()
        {
            var textNotEnoughToFillViewPort = GenerateText(3, 20);
            this.terminal.ReceiveData(textNotEnoughToFillViewPort);

            Assert.AreEqual(0, this.terminal.ViewTop);

            this.terminal.EnableCtrlHomeEnd = true;
            this.terminal.SimulateKey(Keys.Control | Keys.Home);

            Assert.AreEqual(0, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlHomeEndEnabledAndTerminalScrolledToTop_ThenTypingControlDownScrollsToEnd()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            var previousViewTop = this.terminal.ViewTop;

            this.terminal.ScrollToTop();
            Assert.AreEqual(0, this.terminal.ViewTop);

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.End);

            Assert.AreEqual(previousViewTop, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlHomeEndEnabledAndTerminalScrolledToEnd_ThenTypingControlDownIsIgnored()
        {
            var textStraddlingViewPort = GenerateText(100, 20);
            this.terminal.ReceiveData(textStraddlingViewPort);

            var previousViewTop = this.terminal.ViewTop;

            this.terminal.EnableCtrlUpDown = true;
            this.terminal.SimulateKey(Keys.Control | Keys.End);

            Assert.AreEqual(previousViewTop, this.terminal.ViewTop);
            Assert.AreEqual("", this.sendData.ToString());
        }

        [Test]
        public void WhenControlHomeEndDisabled_ThenTypingControlHomeIsIgnored()
        {
            this.terminal.EnableCtrlHomeEnd = false;
            this.terminal.SimulateKey(Keys.Control | Keys.Home);

            Assert.AreEqual("", this.sendData.ToString());
            Assert.IsFalse(this.terminal.IsTextSelected);
        }

        [Test]
        public void WhenControlHomeEndDisabled_ThenTypingControlEndIsIgnored()
        {
            this.terminal.EnableCtrlHomeEnd = false;
            this.terminal.SimulateKey(Keys.Control | Keys.End);

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

        //---------------------------------------------------------------------
        // Select word.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPositionHitsRowThatIsAllWhitespace_ThenSelectWordIsWhitespace()
        {
            this.terminal.ReceiveData("\r\n\r\n\r\n\r\n\r\n\r\n");
            this.terminal.SelectWord(6, 3);
            Assert.AreEqual(
                string.Empty, 
                this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWhitespaceBetweenWords_ThenSelectWordReturnsWhitespace()
        {
            this.terminal.ReceiveData("first  second");

            this.terminal.SelectWord(5, 0);
            Assert.AreEqual("  ", this.terminal.TextSelection);

            this.terminal.SelectWord(6, 0);
            Assert.AreEqual("  ", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWord_ThenSelectWordReturnsWord()
        {
            this.terminal.ReceiveData("first second third");

            this.terminal.SelectWord(6, 0);
            Assert.AreEqual("second", this.terminal.TextSelection);

            this.terminal.SelectWord(10, 0);
            Assert.AreEqual("second", this.terminal.TextSelection);

            this.terminal.SelectWord(11, 0);
            Assert.AreEqual("second", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToEndOfBuffer_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns);
            this.terminal.ReceiveData(lineOfA + "\r\n" + lineOfA);

            this.terminal.SelectWord(0, 0);
            Assert.AreEqual(lineOfA + "\n" + lineOfA, this.terminal.TextSelection);

            this.terminal.SelectWord(this.terminal.Columns - 1, 1);
            Assert.AreEqual(lineOfA + "\n" + lineOfA, this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToPartsOfNextLine_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns);
            this.terminal.ReceiveData(lineOfA + "b c");

            this.terminal.SelectWord(0, 0);
            Assert.AreEqual(lineOfA + "\nb", this.terminal.TextSelection);

            this.terminal.SelectWord(this.terminal.Columns - 1, 0);
            Assert.AreEqual(lineOfA + "\nb", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToEntireNextLine_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns);
            this.terminal.ReceiveData(lineOfA + "\r\n" + lineOfA + " b c");

            this.terminal.SelectWord(0, 0);
            Assert.AreEqual(lineOfA + "\n" + lineOfA, this.terminal.TextSelection);

            this.terminal.SelectWord(this.terminal.Columns - 1, 0);
            Assert.AreEqual(lineOfA + "\n" + lineOfA, this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToStartOfBuffer_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns);
            this.terminal.ReceiveData(lineOfA + "b c");

            this.terminal.SelectWord(0, 1);
            Assert.AreEqual(lineOfA + "\nb", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToEntirePreviousLine_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns);
            this.terminal.ReceiveData("xxx yy zz\r\n" + lineOfA + "b c");

            this.terminal.SelectWord(0, 1);
            Assert.AreEqual(lineOfA + "\nb", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsWordThatExtendsToPartsOfPreviousLine_ThenSelectWordReturnsWord()
        {
            var lineOfA = new string('a', this.terminal.Columns - 2);
            this.terminal.ReceiveData("x " + lineOfA + "b c");

            this.terminal.SelectWord(0, 1);
            Assert.AreEqual(lineOfA + "\nb", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsLastWord_ThenSelectWordReturnsWord()
        {
            this.terminal.ReceiveData("first second third");

            this.terminal.SelectWord(14, 0);
            Assert.AreEqual("third", this.terminal.TextSelection);

            this.terminal.SelectWord(16, 0);
            Assert.AreEqual("third", this.terminal.TextSelection);

            this.terminal.SelectWord(17, 0);
            Assert.AreEqual("third", this.terminal.TextSelection);
        }

        [Test]
        public void WhenPositionHitsFirstWord_ThenSelectWordReturnsWord()
        {
            this.terminal.ReceiveData("first second third");

            this.terminal.SelectWord(0, 0);
            Assert.AreEqual("first", this.terminal.TextSelection);

            this.terminal.SelectWord(2, 0);
            Assert.AreEqual("first", this.terminal.TextSelection);

            this.terminal.SelectWord(4, 0);
            Assert.AreEqual("first", this.terminal.TextSelection);
        }
    }
}
