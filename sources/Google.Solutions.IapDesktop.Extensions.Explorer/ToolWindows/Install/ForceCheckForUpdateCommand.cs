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

using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Install
{
    [MenuCommand(typeof(HelpMenu), Rank = 0x1001)]
    [Service]
    public class ForceCheckForUpdateCommand : CheckForUpdateCommand<IInstall>
    {
        public ForceCheckForUpdateCommand(
            IWin32Window parentWindow,
            IInstall install,
            IUpdatePolicy updatePolicy,
            IReleaseFeed feed,
            ITaskDialog taskDialog,
            IBrowser browser)
            : base(parentWindow, install, updatePolicy, feed, taskDialog, browser)
        {
        }

        protected override bool IsUpdateAdvised(IRelease release)
        {
            //
            // NB. The FeedOptions make sure won't suggest canary updates
            // if the user isn't on the canary track.
            //
            Debug.Assert(!release.IsCanaryRelease || 
                this.FeedOptions == ReleaseFeedOptions.IncludeCanaryReleases);

            if (release.TagVersion == null ||
                release.TagVersion.CompareTo(this.InstalledVersion) <= 0)
            {
                //
                // Installed version is up to date.
                //
                return false;
            }
            else
            {
                //
                // This is a forced update check, ignore the update policy
                // and advise all updates.
                //
                return true;
            }
        }
    }
}
