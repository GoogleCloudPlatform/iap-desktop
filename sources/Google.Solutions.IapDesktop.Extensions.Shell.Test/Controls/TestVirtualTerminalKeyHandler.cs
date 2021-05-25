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
        // Modifiers.
        //---------------------------------------------------------------------

        [Test]
        public void CtrlSpace()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(
                Keys.Control | Keys.Space));

            Assert.AreEqual("\u0000", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Special keys.
        //---------------------------------------------------------------------

        [Test]
        public void Return()
        {
            //
            // NB. This test requires a patched version of vtnetcore. If it fails,
            // you're probably using an unpatched version.
            //

            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return));
            Assert.IsTrue(this.keyHandler.KeyPressed('a'));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Shift));
            Assert.IsTrue(this.keyHandler.KeyPressed('b'));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Return | Keys.Control));

            Assert.AreEqual("\ra\rb\r", this.sendData.ToString());
        }


        //---------------------------------------------------------------------
        // Cursor.
        //---------------------------------------------------------------------

        [Test]
        public void UpArrow()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Up));
            Assert.AreEqual($"{Esc}[A", this.sendData.ToString());
        }

        [Test]
        public void DownArrow()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Down));
            Assert.AreEqual($"{Esc}[B", this.sendData.ToString());
        }

        [Test]
        public void RightArrow()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Right));
            Assert.AreEqual($"{Esc}[C", this.sendData.ToString());
        }

        [Test]
        public void LeftArrow()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Left));
            Assert.AreEqual($"{Esc}[D", this.sendData.ToString());
        }

        [Test]
        public void Home()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Home));
            Assert.AreEqual($"{Esc}[1~", this.sendData.ToString());
        }

        [Test]
        public void End()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.End));
            Assert.AreEqual($"{Esc}[4~", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Modifiers.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTypingCtrlChar_ThenKeystrokeIsSent()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Control | Keys.A));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Control | Keys.Z));

            Assert.AreEqual("\u0001\u001a", this.sendData.ToString());
        }

        [Test]
        public void WhenTypingAltChar_ThenKeystrokeIsSent()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Alt | Keys.A));
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Alt | Keys.Z));

            Assert.AreEqual("\u001ba\u001bz", this.sendData.ToString());
        }

        //---------------------------------------------------------------------
        // Numpad & Function Keys.
        //---------------------------------------------------------------------

        [Test]
        public void Backspace()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Back));
            Assert.AreEqual("\u007f", this.sendData.ToString());
        }

        [Test]
        [Ignore("Not supported by vtnetcore")]
        public void Pause()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Pause));
            Assert.AreEqual("\u001a", this.sendData.ToString());
        }

        [Test]
        public void Escape()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Escape));
            Assert.AreEqual(Esc + Esc, this.sendData.ToString());
        }

        [Test]
        public void Insert()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Insert));
            Assert.AreEqual($"{Esc}[2~", this.sendData.ToString());
        }

        [Test]
        public void Delete()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Delete));
            Assert.AreEqual($"{Esc}[3~", this.sendData.ToString());
        }

        [Test]
        public void PageUp()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageUp));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
        }

        [Test]
        public void Prior()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Prior));
            Assert.AreEqual($"{Esc}[5~", this.sendData.ToString());
        }

        [Test]
        public void PageDown()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.PageDown));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
        }

        [Test]
        public void Next()
        {
            Assert.IsTrue(this.keyHandler.KeyDown(Keys.Next));
            Assert.AreEqual($"{Esc}[6~", this.sendData.ToString());
        }

        [Test]
        public void FunctionKeyInLowerRange(
            [Range(1, 5)] int functionKey
            )
        {
            Assert.IsTrue(this.keyHandler.KeyDown((Keys)(Keys.F1 + functionKey - 1)));
            Assert.AreEqual($"{Esc}[{10 + functionKey}~", this.sendData.ToString());
        }

        [Test]
        public void FunctionKeyInMiddleRange(
            [Range(6, 10)] int functionKey
            )
        {
            Assert.IsTrue(this.keyHandler.KeyDown((Keys)(Keys.F1 + functionKey - 1)));
            Assert.AreEqual($"{Esc}[{11 + functionKey}~", this.sendData.ToString());
        }

        [Test]
        public void FunctionKeyInUpperRange(
            [Range(11, 12)] int functionKey
            )
        {
            Assert.IsTrue(this.keyHandler.KeyDown((Keys)(Keys.F1 + functionKey - 1)));
            Assert.AreEqual($"{Esc}[{12 + functionKey}~", this.sendData.ToString());
        }
    }
}
