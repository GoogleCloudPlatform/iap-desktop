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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Profile
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x1000)]
    [Service]
    public class SignOutCommand : ProfileCommandBase
    {
        private readonly IInstall install;
        private readonly IAuthorization authorization;
        private readonly IMainWindow mainWindow;
        private readonly IConfirmationDialog confirmationDialog;

        public SignOutCommand(
            IInstall install,
            IAuthorization authorization,
            IMainWindow mainWindow,
            IConfirmationDialog confirmationDialog)
            : base("Sign &out and exit")
        {
            this.install = install.ExpectNotNull(nameof(install));
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.mainWindow = mainWindow.ExpectNotNull(nameof(mainWindow));
            this.confirmationDialog = confirmationDialog.ExpectNotNull(nameof(confirmationDialog));
        }

        public override void Execute(IUserProfile profile)
        {
            //
            // Terminate the session.
            //
            this.authorization.Session.Terminate();

            if (!profile.IsDefault)
            {
                if (this.confirmationDialog.Confirm(
                    this.mainWindow,
                    $"Would you like to keep the current profile '{profile.Name}'?",
                    "Keep profile",
                    "Sign out") == DialogResult.No)
                {
                    //
                    // Delete current profile.
                    //
                    // Because of the single-instance behavior of this app, we know
                    // (with reasonable certainty) that this is the only instance 
                    // that's currently using this profile. Therefore, it's safe
                    // to perform the deletion here.
                    //
                    // If we provided a "Delete profile" option in the profile
                    // selection, we couldn't know for sure that the profile
                    // isn't currently being used by another instance.
                    //
                    UserProfile.DeleteProfile(this.install, profile.Name);

                    //
                    // Perform a hard exit to avoid touching the
                    // registry keys (which are now marked for deletion)
                    // again.
                    //
                    Environment.Exit(0);
                }
            }

            //
            // Exit gracefully.
            //
            this.mainWindow.Close();
        }
    }
}
