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
using VtNetCore.VirtualTerminal;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Controls
{
    [TestFixture]
    public class TestVirtualTerminalKeyHandler
    {
        private readonly string Esc = "\u001b";

        private VirtualTerminalController controller;
        private VirtualTerminalKeyHandler keyHandler;
        private StringBuilder sendData;

        [SetUp]
        public void SetUp()
        {
            this.sendData = new StringBuilder();
            this.controller = new VirtualTerminalController();
            this.controller.SendData += (sender, args) =>
            {
                this.sendData.Append(Encoding.UTF8.GetString(args.Data));
            };
            this.keyHandler = new VirtualTerminalKeyHandler(this.controller);
        }

        //---------------------------------------------------------------------
        // Function keys.
        //---------------------------------------------------------------------

        [Test]
        public void F1()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1));
            Assert.AreEqual($"{Esc}[11~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Shift));
            Assert.AreEqual($"{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Control));
            Assert.AreEqual($"{Esc}[11~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[11~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F2()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2));
            Assert.AreEqual($"{Esc}[12~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Shift));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Control));
            Assert.AreEqual($"{Esc}[12~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[12~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F3()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3));
            Assert.AreEqual($"{Esc}[13~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Shift));
            Assert.AreEqual($"{Esc}[25~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Control));
            Assert.AreEqual($"{Esc}[13~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[13~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F4()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4));
            Assert.AreEqual($"{Esc}[14~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Shift));
            Assert.AreEqual($"{Esc}[26~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Control));
            Assert.AreEqual($"{Esc}[14~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[14~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F5()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5));
            Assert.AreEqual($"{Esc}[15~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Shift));
            Assert.AreEqual($"{Esc}[28~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Control));
            Assert.AreEqual($"{Esc}[15~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[15~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F6()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6));
            Assert.AreEqual($"{Esc}[17~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Shift));
            Assert.AreEqual($"{Esc}[29~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Control));
            Assert.AreEqual($"{Esc}[17~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[17~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F7()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7));
            Assert.AreEqual($"{Esc}[18~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Shift));
            Assert.AreEqual($"{Esc}[31~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Control));
            Assert.AreEqual($"{Esc}[18~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[18~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F8()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8));
            Assert.AreEqual($"{Esc}[19~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Shift));
            Assert.AreEqual($"{Esc}[32~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Control));
            Assert.AreEqual($"{Esc}[19~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[19~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F9()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9));
            Assert.AreEqual($"{Esc}[20~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Shift));
            Assert.AreEqual($"{Esc}[33~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Control));
            Assert.AreEqual($"{Esc}[20~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[20~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F10()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10));
            Assert.AreEqual($"{Esc}[21~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Shift));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Control));
            Assert.AreEqual($"{Esc}[21~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[21~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F11()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11));
            Assert.AreEqual($"{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Shift));
            Assert.AreEqual($"{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Control));
            Assert.AreEqual($"{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F12()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Shift));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Control));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();
        }

        //---------------------------------------------------------------------
        // Arrow keys.
        //---------------------------------------------------------------------

        [Test]
        public void Up()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up));
            Assert.AreEqual($"{Esc}[A", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up | Keys.Shift));
            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up | Keys.Control));
            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[A", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up));
            Assert.AreEqual($"{Esc}OA", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Down()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down));
            Assert.AreEqual($"{Esc}[B", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down | Keys.Shift));
            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down | Keys.Control));
            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[B", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down));
            Assert.AreEqual($"{Esc}OB", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Right()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right));
            Assert.AreEqual($"{Esc}[C", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right | Keys.Shift));
            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right | Keys.Control));
            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[C", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right));
            Assert.AreEqual($"{Esc}OC", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Left()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left));
            Assert.AreEqual($"{Esc}[D", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left | Keys.Shift));
            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left | Keys.Control));
            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[D", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left));
            Assert.AreEqual($"{Esc}OD", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Home()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home));
            Assert.AreEqual($"{Esc}[1~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home | Keys.Shift));
            Assert.AreEqual($"{Esc}[1~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Home | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[1~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home));
            Assert.AreEqual($"{Esc}[1~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void End()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End));
            Assert.AreEqual($"{Esc}[4~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End | Keys.Shift));
            Assert.AreEqual($"{Esc}[4~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.End | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[4~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End));
            Assert.AreEqual($"{Esc}[4~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Insert()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert));
            Assert.AreEqual($"{Esc}[2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Insert | Keys.Shift));
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Insert | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[2~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert));
            Assert.AreEqual($"{Esc}[2~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Delete()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete));
            Assert.AreEqual($"{Esc}[3~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete | Keys.Shift));
            Assert.AreEqual($"{Esc}[3~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Delete | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[3~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete));
            Assert.AreEqual($"{Esc}[3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void PageUp()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp | Keys.Shift));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.PageUp | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Prior()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior | Keys.Shift));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Prior | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void PageDown()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown | Keys.Shift));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.PageDown | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Next()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next | Keys.Shift));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Next | Keys.Control));
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next | Keys.Alt));
            Assert.AreEqual($"{Esc}{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
            this.sendData.Clear();
        }

        //---------------------------------------------------------------------
        // Main keyboard keys.
        //---------------------------------------------------------------------

        [Test]
        public void Back()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back));
            Assert.AreEqual("\u007f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back | Keys.Shift));
            Assert.AreEqual($"\b", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back | Keys.Control));
            Assert.AreEqual("\u007f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Back | Keys.Alt));
        }


        [Test]
        public void Tab()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab));
            Assert.AreEqual("\t", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab | Keys.Shift));
            Assert.AreEqual($"{Esc}[Z", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Tab | Keys.Control));

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Tab | Keys.Alt));
        }

        [Test]
        public void Return()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Shift));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Control));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Alt));
            Assert.AreEqual($"{Esc}\r", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Enter()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Enter));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Enter | Keys.Shift));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Enter | Keys.Control));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Enter | Keys.Alt));
            Assert.AreEqual($"{Esc}\r", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Escape()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape));
            Assert.AreEqual($"{Esc}{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape | Keys.Shift));
            Assert.AreEqual($"{Esc}{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape | Keys.Control));
            Assert.AreEqual($"{Esc}{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Escape | Keys.Alt));
        }

        [Test]
        public void Space()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Space));

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Space | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Space | Keys.Control));
            Assert.AreEqual($"\u0000", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Space | Keys.Alt));
            Assert.AreEqual($"{Esc} ", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Letter(
            [Range(0, 25)] int offset)
        {
            var key = (Keys)(Keys.A + offset);
            Assert.IsFalse(this.keyHandler.KeyDown(key));

            Assert.IsFalse(this.keyHandler.KeyDown(key | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(key | Keys.Control));
            Assert.AreEqual(((char)(offset + 1)).ToString(), this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(key | Keys.Alt));
            Assert.AreEqual($"{Esc}" + (char)('a' + offset), this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        [Ignore("Not supported by vtnetcore")]
        public void Pause()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Pause));
            Assert.AreEqual("\u001a", this.sendData.ToString());
        }
    }
}
