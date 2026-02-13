//
// Copyright 2026 Google LLC
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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Auth;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Profile
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x200)]
    [Service]
    public class CreateProfileCommand : ProfileCommandBase
    {
        private readonly IInstall install;
        private readonly IMainWindow mainWindow;
        private readonly WindowActivator<NewProfileView, NewProfileViewModel, IDialogTheme>
            dialogActivator;

        public CreateProfileCommand(
            IInstall install,
            IMainWindow mainWindow, 
            WindowActivator<NewProfileView, NewProfileViewModel, IDialogTheme> dialogActivator)
            : base("New &profile...")
        {
            this.install = install;
            this.mainWindow = mainWindow;
            this.dialogActivator = dialogActivator;
        }

        public override void Execute(IUserProfile _)
        {
            using (var dialog = this.dialogActivator.CreateDialog())
            {
                if (dialog.ShowDialog(this.mainWindow) == DialogResult.OK)
                {
                    using (var profile = this.install
                        .CreateProfile(dialog.ViewModel.ProfileName))
                    {
                        profile.Launch();
                    }
                }
            }
        }
    }
}
