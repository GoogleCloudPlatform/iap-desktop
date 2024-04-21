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

using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using NUnit.Framework;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Controls
{
    [TestFixture]
    public class TestVirtualTerminalKeyHandler
    {
        private const string Esc = "\u001b";
        private const string Ss3 = Esc + "O";

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
            Assert.AreEqual($"{Ss3}P", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2P", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5P", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F1 | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3P", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F2()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2));
            Assert.AreEqual($"{Ss3}Q", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2Q", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5Q", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F2 | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3Q", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F3()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3));
            Assert.AreEqual($"{Ss3}R", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2R", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5R", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F3 | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3R", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F4()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4));
            Assert.AreEqual($"{Ss3}S", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2S", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5S", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F4 | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3S", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F5()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5));
            Assert.AreEqual($"{Esc}[15~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Shift));
            Assert.AreEqual($"{Esc}[15;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Control));
            Assert.AreEqual($"{Esc}[15;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F5 | Keys.Alt));
            Assert.AreEqual($"{Esc}[15;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F6()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6));
            Assert.AreEqual($"{Esc}[17~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Shift));
            Assert.AreEqual($"{Esc}[17;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Control));
            Assert.AreEqual($"{Esc}[17;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F6 | Keys.Alt));
            Assert.AreEqual($"{Esc}[17;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F7()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7));
            Assert.AreEqual($"{Esc}[18~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Shift));
            Assert.AreEqual($"{Esc}[18;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Control));
            Assert.AreEqual($"{Esc}[18;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F7 | Keys.Alt));
            Assert.AreEqual($"{Esc}[18;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F8()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8));
            Assert.AreEqual($"{Esc}[19~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Shift));
            Assert.AreEqual($"{Esc}[19;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Control));
            Assert.AreEqual($"{Esc}[19;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F8 | Keys.Alt));
            Assert.AreEqual($"{Esc}[19;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F9()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9));
            Assert.AreEqual($"{Esc}[20~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Shift));
            Assert.AreEqual($"{Esc}[20;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Control));
            Assert.AreEqual($"{Esc}[20;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F9 | Keys.Alt));
            Assert.AreEqual($"{Esc}[20;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F10()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10));
            Assert.AreEqual($"{Esc}[21~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Shift));
            Assert.AreEqual($"{Esc}[21;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Control));
            Assert.AreEqual($"{Esc}[21;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F10 | Keys.Alt));
            Assert.AreEqual($"{Esc}[21;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F11()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11));
            Assert.AreEqual($"{Esc}[23~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Shift));
            Assert.AreEqual($"{Esc}[23;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Control));
            Assert.AreEqual($"{Esc}[23;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F11 | Keys.Alt));
            Assert.AreEqual($"{Esc}[23;3~", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void F12()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12));
            Assert.AreEqual($"{Esc}[24~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Shift));
            Assert.AreEqual($"{Esc}[24;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Control));
            Assert.AreEqual($"{Esc}[24;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.F12 | Keys.Alt));
            Assert.AreEqual($"{Esc}[24;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[1;2A", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5A", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3A", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[1;2B", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5B", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3B", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[1;2C", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5C", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3C", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[1;2D", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5D", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3D", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[H", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2H", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5H", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3H", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home));
            Assert.AreEqual($"{Esc}OH", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void End()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End));
            Assert.AreEqual($"{Esc}[F", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End | Keys.Shift));
            Assert.AreEqual($"{Esc}[1;2F", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End | Keys.Control));
            Assert.AreEqual($"{Esc}[1;5F", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End | Keys.Alt));
            Assert.AreEqual($"{Esc}[1;3F", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.EnableApplicationCursorKeys(true);
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End));
            Assert.AreEqual($"{Esc}OF", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void Insert()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert));
            Assert.AreEqual($"{Esc}[2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert | Keys.Shift));
            Assert.AreEqual($"{Esc}[2;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert | Keys.Control));
            Assert.AreEqual($"{Esc}[2;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert | Keys.Alt));
            Assert.AreEqual($"{Esc}[2;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[3;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete | Keys.Control));
            Assert.AreEqual($"{Esc}[3;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete | Keys.Alt));
            Assert.AreEqual($"{Esc}[3;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[5;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp | Keys.Control));
            Assert.AreEqual($"{Esc}[5;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp | Keys.Alt));
            Assert.AreEqual($"{Esc}[5;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[5;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior | Keys.Control));
            Assert.AreEqual($"{Esc}[5;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior | Keys.Alt));
            Assert.AreEqual($"{Esc}[5;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[6;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown | Keys.Control));
            Assert.AreEqual($"{Esc}[6;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown | Keys.Alt));
            Assert.AreEqual($"{Esc}[6;3~", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}[6;2~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next | Keys.Control));
            Assert.AreEqual($"{Esc}[6;5~", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next | Keys.Alt));
            Assert.AreEqual($"{Esc}[6;3~", this.sendData.ToString());
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
            Assert.AreEqual($"\u007f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back | Keys.Control));
            Assert.AreEqual("\b", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back | Keys.Alt));
            Assert.AreEqual($"{Esc}\u007f", this.sendData.ToString());
            this.sendData.Clear();
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

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab | Keys.Control));
            Assert.AreEqual("\t", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Tab | Keys.Alt));

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab));
            Assert.AreEqual("\t", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab | Keys.Shift));
            Assert.AreEqual($"{Esc}[Z", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Tab | Keys.Control));
            Assert.AreEqual($"{Esc}[27;5;9", this.sendData.ToString());
            this.sendData.Clear();

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

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return));
            Assert.AreEqual("\r", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Shift));
            Assert.AreEqual($"{Esc}[27;2;13", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Control));
            Assert.AreEqual($"{Esc}[27;5;13", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Alt));
            Assert.AreEqual($"{Esc}[27;3;13", this.sendData.ToString());
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
            Assert.AreEqual($"{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape | Keys.Shift));
            Assert.AreEqual($"{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape | Keys.Control));
            Assert.AreEqual($"{Esc}", this.sendData.ToString());
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
        public void Pause()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Pause));
            Assert.AreEqual("\u001a", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Pause | Keys.Shift));
            Assert.AreEqual("\u001a", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Pause | Keys.Control));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Pause | Keys.Alt));
            Assert.AreEqual($"{Esc}\u001a", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D0()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D0));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D0 | Keys.Shift));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D0 | Keys.Control));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D0 | Keys.Alt));
            Assert.AreEqual($"{Esc}0", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D0));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D0 | Keys.Shift));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D0 | Keys.Control));
            Assert.AreEqual($"{Esc}[27;5;48", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D0 | Keys.Alt));
            Assert.AreEqual($"{Esc}0", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D1()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D1));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D1 | Keys.Shift));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D1 | Keys.Control));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D1 | Keys.Alt));
            Assert.AreEqual($"{Esc}1", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D1));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D1 | Keys.Shift));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D1 | Keys.Control));
            Assert.AreEqual($"{Esc}[27;5;49", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D1 | Keys.Alt));
            Assert.AreEqual($"{Esc}1", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D2()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D2));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D2 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D2 | Keys.Control));
            Assert.AreEqual($"\u0000", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D2 | Keys.Alt));
            Assert.AreEqual($"{Esc}2", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D2));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D2 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D2 | Keys.Control));
            Assert.AreEqual($"\u0000", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D2 | Keys.Alt));
            Assert.AreEqual($"{Esc}2", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D3()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D3));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D3 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D3 | Keys.Control));
            Assert.AreEqual($"{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D3 | Keys.Alt));
            Assert.AreEqual($"{Esc}3", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D3));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D3 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D3 | Keys.Control));
            Assert.AreEqual($"{Esc}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D3 | Keys.Alt));
            Assert.AreEqual($"{Esc}3", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D4()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D4));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D4 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D4 | Keys.Control));
            Assert.AreEqual($"\u001c", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D4 | Keys.Alt));
            Assert.AreEqual($"{Esc}4", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D4));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D4 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D4 | Keys.Control));
            Assert.AreEqual($"\u001c", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D4 | Keys.Alt));
            Assert.AreEqual($"{Esc}4", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D5()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D5));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D5 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D5 | Keys.Control));
            Assert.AreEqual($"\u001d", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D5 | Keys.Alt));
            Assert.AreEqual($"{Esc}5", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D5));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D5 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D5 | Keys.Control));
            Assert.AreEqual($"\u001d", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D5 | Keys.Alt));
            Assert.AreEqual($"{Esc}5", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D6()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D6));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D6 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D6 | Keys.Control));
            Assert.AreEqual($"\u001e", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D6 | Keys.Alt));
            Assert.AreEqual($"{Esc}6", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D6));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D6 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D6 | Keys.Control));
            Assert.AreEqual($"\u001e", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D6 | Keys.Alt));
            Assert.AreEqual($"{Esc}6", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D7()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D7));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D7 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D7 | Keys.Control));
            Assert.AreEqual($"\u001f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D7 | Keys.Alt));
            Assert.AreEqual($"{Esc}7", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D7));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D7 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D7 | Keys.Control));
            Assert.AreEqual($"\u001f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D7 | Keys.Alt));
            Assert.AreEqual($"{Esc}7", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D8()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D8));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D8 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D8 | Keys.Control));
            Assert.AreEqual($"\u007f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D8 | Keys.Alt));
            Assert.AreEqual($"{Esc}8", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D8));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D8 | Keys.Shift));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D8 | Keys.Control));
            Assert.AreEqual($"\u007f", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D8 | Keys.Alt));
            Assert.AreEqual($"{Esc}8", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void D9()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D9));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D9 | Keys.Shift));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D9 | Keys.Control));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D9 | Keys.Alt));
            Assert.AreEqual($"{Esc}9", this.sendData.ToString());
            this.sendData.Clear();

            this.controller.ModifyOtherKeys = ModifyOtherKeysMode.EnabledWithExceptions;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D9));
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.D9 | Keys.Shift));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D9 | Keys.Control));
            Assert.AreEqual($"{Esc}[27;5;57", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.D9 | Keys.Alt));
            Assert.AreEqual($"{Esc}9", this.sendData.ToString());
            this.sendData.Clear();
        }

        //---------------------------------------------------------------------
        // Num pad keys.
        //---------------------------------------------------------------------

        [Test]
        public void NumPad0()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad0));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad0 | Keys.Control));
            Assert.AreEqual($"0", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad0 | Keys.Alt));
            Assert.AreEqual($"{Esc}0", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad1()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad1));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad1 | Keys.Control));
            Assert.AreEqual($"1", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad1 | Keys.Alt));
            Assert.AreEqual($"{Esc}1", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad2()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad2));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad2 | Keys.Control));
            Assert.AreEqual($"2", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad2 | Keys.Alt));
            Assert.AreEqual($"{Esc}2", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad3()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad3));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad3 | Keys.Control));
            Assert.AreEqual($"3", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad3 | Keys.Alt));
            Assert.AreEqual($"{Esc}3", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad4()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad4));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad4 | Keys.Control));
            Assert.AreEqual($"4", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad4 | Keys.Alt));
            Assert.AreEqual($"{Esc}4", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad5()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad5));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad5 | Keys.Control));
            Assert.AreEqual($"5", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad5 | Keys.Alt));
            Assert.AreEqual($"{Esc}5", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad6()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad6));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad6 | Keys.Control));
            Assert.AreEqual($"6", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad6 | Keys.Alt));
            Assert.AreEqual($"{Esc}6", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad7()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad7));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad7 | Keys.Control));
            Assert.AreEqual($"7", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad7 | Keys.Alt));
            Assert.AreEqual($"{Esc}7", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad8()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad8));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad8 | Keys.Control));
            Assert.AreEqual($"8", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad8 | Keys.Alt));
            Assert.AreEqual($"{Esc}8", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPad9()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.NumPad9));

            // NB. Skip Shift as that disables Numpad.

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad9 | Keys.Control));
            Assert.AreEqual($"9", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.NumPad9 | Keys.Alt));
            Assert.AreEqual($"{Esc}9", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPadDivide()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Divide));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Divide | Keys.Shift));
            Assert.AreEqual($"/", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Divide | Keys.Control));
            Assert.AreEqual($"/", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Divide | Keys.Alt));
            Assert.AreEqual($"/", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPadMultiply()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Multiply));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Multiply | Keys.Shift));
            Assert.AreEqual($"*", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Multiply | Keys.Control));
            Assert.AreEqual($"*", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Multiply | Keys.Alt));
            Assert.AreEqual($"*", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPadSubtract()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Subtract));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Subtract | Keys.Shift));
            Assert.AreEqual(string.Empty, this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Subtract | Keys.Control));
            Assert.AreEqual($"-", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Subtract | Keys.Alt));
            Assert.AreEqual($"-", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPadAdd()
        {
            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Add));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Add | Keys.Shift));
            Assert.AreEqual(string.Empty, this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Add | Keys.Control));
            Assert.AreEqual($"+", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Add | Keys.Alt));
            Assert.AreEqual($"+", this.sendData.ToString());
            this.sendData.Clear();
        }

        [Test]
        public void NumPadDecimal()
        {
            var separator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;

            Assert.IsFalse(this.keyHandler.KeyDown(Keys.Decimal));

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Decimal | Keys.Shift));
            Assert.AreEqual($"{separator}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Decimal | Keys.Control));
            Assert.AreEqual($"{separator}", this.sendData.ToString());
            this.sendData.Clear();

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Decimal | Keys.Alt));
            Assert.AreEqual($"{Esc}{separator}", this.sendData.ToString());
            this.sendData.Clear();
        }
    }
}
