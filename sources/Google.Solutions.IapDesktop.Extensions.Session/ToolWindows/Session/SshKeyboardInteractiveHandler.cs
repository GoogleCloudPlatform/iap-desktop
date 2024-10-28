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
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Ssh;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    internal class SshKeyboardInteractiveHandler : IKeyboardInteractiveHandler
    {
        private readonly IWin32Window owner;
        private readonly IInputDialog inputDialog;
        private readonly InstanceLocator instance;

        /// <summary>
        /// Check if the caller thread is different from the UI thread.
        /// </summary>
        private bool InvokeRequired
        {
            get => (this.owner as ISynchronizeInvoke)?.InvokeRequired == true;
        }

        private void ValidateInputIsNotEmpty(
            string input,
            out bool valid,
            out string? warning)
        {
            valid = !string.IsNullOrEmpty(input);
            warning = null;
        }

        public SshKeyboardInteractiveHandler(
            IWin32Window owner,
            IInputDialog inputDialog,
            InstanceLocator instance)
        {
            this.owner = owner;
            this.inputDialog = inputDialog;
            this.instance = instance;
        }

        //----------------------------------------------------------------------
        // IKeyboardInteractiveHandler.
        //----------------------------------------------------------------------

        public string? Prompt(string caption, string instruction, string prompt, bool echo)
        {
            Debug.Assert(!this.InvokeRequired, "On UI thread");

            if (this.inputDialog.Prompt(
                this.owner,
                new InputDialogParameters()
                {
                    Title = this.instance.Name,
                    Caption = caption,
                    IsPassword = !echo,
                    Message = prompt,
                    Validate = ValidateInputIsNotEmpty
                },
                out var userInput) == DialogResult.OK && userInput != null)
            {
                //
                // NB. The input dialog won't give us a null or empty result
                //     because of the validation callback we use.
                //
                Debug.Assert(!string.IsNullOrEmpty(userInput));
                userInput = userInput.Trim();

                //
                // Strip:
                //  - spaces between group of digits (g.co/sc)
                //  - "G-" prefix (text messages)
                //
                if (userInput.StartsWith("g-", StringComparison.OrdinalIgnoreCase))
                {
                    userInput = userInput.Substring(2);
                }

                return userInput.Replace(" ", string.Empty);
            }
            else
            {
                throw new OperationCanceledException(); // TODO: Verify that OperationCanceledException is handled correctly.
            }
        }

        public IPasswordCredential PromptForCredentials(string username)
        {
            Debug.Assert(!this.InvokeRequired, "On UI thread");

            if (this.inputDialog.Prompt(
                this.owner,
                new InputDialogParameters()
                {
                    Title = this.instance.Name,
                    Caption = "Enter password for " + username,
                    IsPassword = true,
                    Message = "These credentials will be used to connect to " + this.instance.Name,
                    Validate = ValidateInputIsNotEmpty
                },
                out var userInput) == DialogResult.OK && userInput != null)
            {
                return new StaticPasswordCredential(username, userInput.Trim());
            }
            else
            {
                throw new OperationCanceledException();
            }
        }
    }
}
