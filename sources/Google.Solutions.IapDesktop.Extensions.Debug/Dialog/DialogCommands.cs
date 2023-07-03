﻿//
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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Debug.Dialog
{
    [MenuCommand(typeof(DebugMenu), Rank = 0x400)]
    [Service]
    public class PromptForUsername : MenuCommandBase<DebugMenu.Context>
    {
        private readonly IWin32Window window;
        private readonly ICredentialDialog credentialDialog;

        public PromptForUsername(
            IWin32Window window,
            ICredentialDialog credentialDialog)
            : base("&Prompt for username")
        {
            this.window = window;
            this.credentialDialog = credentialDialog;
        }

        protected override bool IsAvailable(DebugMenu.Context context)
        {
            return true;
        }

        protected override bool IsEnabled(DebugMenu.Context context)
        {
            return true;
        }

        public override void Execute(DebugMenu.Context context)
        {
            this.credentialDialog.PromptForUsername(
                this.window,
                "This is a caption",
                "This is a message",
                out var _);
        }
    }
}
