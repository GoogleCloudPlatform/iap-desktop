//
// Copyright 2019 Google LLC
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
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Services.Workflows
{
    public partial class GenerateCredentialsDialog : Form
    {
        // SAM usernames do not permit these characters, see
        // https://docs.microsoft.com/en-us/windows/desktop/adschema/a-samaccountname
        private readonly string DisallowsCharactersInUsername = "\"/\\[]:;|=,+*?<>";


        public GenerateCredentialsDialog()
        {
            InitializeComponent();
        }

        private void usernameText_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Cancel any keypresses of disallowed characters.
            e.Handled = DisallowsCharactersInUsername.IndexOf(e.KeyChar) >= 0;
        }

        internal string PromptForUsername(
            IWin32Window owner,
            string suggestedUsername)
        {
            this.usernameText.Text = suggestedUsername;

            if (ShowDialog(owner) == DialogResult.OK &&
                !String.IsNullOrWhiteSpace(this.usernameText.Text))
            {
                return this.usernameText.Text;
            }
            else
            {
                return null;
            }
        }
    }
}
