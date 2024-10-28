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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
using Google.Solutions.Ssh;
using Moq;
using NUnit.Framework;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
{
    [TestFixture]
    public class TestSshKeyboardInteractiveHandler
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //----------------------------------------------------------------------
        // Prompt.
        //----------------------------------------------------------------------

        [Test]
        public void Prompt_WhenUserCancels()
        {
            var owner = new Mock<IWin32Window>().Object;

            var inputDialog = new Mock<IInputDialog>();
            string? input;
            inputDialog.Setup(
                d => d.Prompt(
                    owner,
                    It.Is<InputDialogParameters>(p => !p.IsPassword),
                    out input))
                .Returns(DialogResult.Cancel);

            var handler = new SshKeyboardInteractiveHandler(
                owner,
                inputDialog.Object,
                SampleLocator);

            Assert.Throws<OperationCanceledException>(
                () => handler.Prompt("caption", "instruction", "prompt", true));
        }

        [Test]
        public void Prompt_WhenInputPrefixed(
            [Values("g-123", "G-123", "  g-123  ")] string prefixedInput)
        {
            var owner = new Mock<IWin32Window>().Object;

            var inputDialog = new Mock<IInputDialog>();
            var input = prefixedInput;
            inputDialog.Setup(
                d => d.Prompt(
                    owner,
                    It.Is<InputDialogParameters>(p => !p.IsPassword),
                    out input))
                .Returns(DialogResult.OK);

            var handler = new SshKeyboardInteractiveHandler(
                owner,
                inputDialog.Object,
                SampleLocator);

            Assert.AreEqual(
                "123",
                handler.Prompt("caption", "instruction", "prompt", true));
        }

        [Test]
        public void Prompt_WhenInputNotPrefixed(
            [Values("123", " \t123 \r\n ")] string prefixedInput)
        {
            var owner = new Mock<IWin32Window>().Object;

            var inputDialog = new Mock<IInputDialog>();
            var input = prefixedInput;
            inputDialog.Setup(
                d => d.Prompt(
                    owner,
                    It.Is<InputDialogParameters>(p => !p.IsPassword),
                    out input))
                .Returns(DialogResult.OK);

            var handler = new SshKeyboardInteractiveHandler(
                owner,
                inputDialog.Object,
                SampleLocator);

            Assert.AreEqual(
                "123",
                handler.Prompt("caption", "instruction", "prompt", true));
        }

        //----------------------------------------------------------------------
        // PromptForCredentials.
        //----------------------------------------------------------------------

        [Test]
        public void PromptForCredentials_WhenUserCancels()
        {
            var owner = new Mock<IWin32Window>().Object;

            var inputDialog = new Mock<IInputDialog>();
            string? input;
            inputDialog.Setup(
                d => d.Prompt(
                    owner,
                    It.Is<InputDialogParameters>(p => p.IsPassword),
                    out input))
                .Returns(DialogResult.Cancel);

            var handler = new SshKeyboardInteractiveHandler(
                owner,
                inputDialog.Object,
                SampleLocator);

            Assert.Throws<OperationCanceledException>(
                () => handler.PromptForCredentials("username"));
        }

        [Test]
        public void PromptForCredentials()
        {
            var owner = new Mock<IWin32Window>().Object;

            var inputDialog = new Mock<IInputDialog>();
            var input = "password";
            inputDialog.Setup(
                d => d.Prompt(
                    owner,
                    It.Is<InputDialogParameters>(p => p.IsPassword),
                    out input))
                .Returns(DialogResult.OK);

            var handler = new SshKeyboardInteractiveHandler(
                owner,
                inputDialog.Object,
                SampleLocator);

            var credentials = handler.PromptForCredentials("username");
            Assert.IsInstanceOf<StaticPasswordCredential>(credentials);
            Assert.AreEqual("username", ((StaticPasswordCredential)credentials).Username);
            Assert.AreEqual("password", ((StaticPasswordCredential)credentials).Password.AsClearText());
        }
    }
}
