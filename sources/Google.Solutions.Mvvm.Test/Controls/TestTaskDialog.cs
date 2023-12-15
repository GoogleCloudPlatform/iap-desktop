//
// Copyright 2023 Google LLC
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

using Google.Solutions.Common.Interop;
using Google.Solutions.Mvvm.Controls;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Controls.TaskDialog;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestTaskDialog
    {
        [Test]
        public void WhenButtonsEmpty_ThenShowDialogThrowsException()
        {
            var parameters = new TaskDialogParameters("heading", "caption", "text");
            Assert.Throws<InvalidOperationException>(
                () => new TaskDialog().ShowDialog(null, parameters));
        }

        //---------------------------------------------------------------------
        // Standard buttons.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOkClicked_ThenShowDialogReturnsOk()
        {
            var parameters = new TaskDialogParameters("heading", "caption", "text");
            parameters.Buttons.Add(TaskDialogStandardButton.OK);
            parameters.Buttons.Add(TaskDialogStandardButton.OK);
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            void taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.AreEqual(0x0009, config.dwCommonButtons);
                Assert.IsNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.OK.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = false;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.OK, dialog.ShowDialog(null, parameters));
        }

        [Test]
        public void WhenCancelClicked_ThenShowDialogReturnsCancel()
        {
            var parameters = new TaskDialogParameters("heading", "caption", "text");
            parameters.Buttons.Add(TaskDialogStandardButton.Yes);
            parameters.Buttons.Add(TaskDialogStandardButton.No);
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            void taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.AreEqual(0x000E, config.dwCommonButtons);
                Assert.IsNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.Cancel.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = false;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.Cancel, dialog.ShowDialog(null, parameters));
        }

        //---------------------------------------------------------------------
        // Command link buttons.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCommandLinkButtonClicked_ThenShowDialogInvokesHandler()
        {
            var parameters = new TaskDialogParameters("heading", "caption", "text");
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            var yes = new TaskDialogCommandLinkButton("Yes", DialogResult.Yes);
            var no = new TaskDialogCommandLinkButton("No", DialogResult.No);
            
            parameters.Buttons.Add(yes);
            parameters.Buttons.Add(no);

            int clicks = 0;
            no.Click += (s, e) => clicks++;

            void taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.IsNull(config.pszVerificationText);
                Assert.IsNotNull(config.cButtons);
                Assert.AreEqual(2, config.cButtons);

                buttonPressed = TaskDialog.CommandLinkIdOffset + 1; // No
                radioButtonPressed = -1;
                verificationFlagChecked = false;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.No, dialog.ShowDialog(null, parameters));
            Assert.AreEqual(1, clicks);
        }

        //---------------------------------------------------------------------
        // Verification checkbox.
        //---------------------------------------------------------------------

        [Test]
        public void WhenVerificationCheckboxChecked_ThenShowDialogUpdatesParameters()
        {
            var parameters = new TaskDialogParameters("heading", "caption", "text")
            {
                VerificationCheckBox = new TaskDialogVerificationCheckBox("check me")
            };

            parameters.Buttons.Add(TaskDialogStandardButton.OK);

            void taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.IsNotNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.OK.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = true;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            dialog.ShowDialog(null, parameters);

            Assert.IsTrue(parameters.VerificationCheckBox.Checked);
        }
    }
}
