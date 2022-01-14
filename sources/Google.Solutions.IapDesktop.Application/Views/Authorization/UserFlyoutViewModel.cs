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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Authorization;

namespace Google.Solutions.IapDesktop.Application.Views.Authorization
{
    public class UserFlyoutViewModel : ViewModelBase
    {
        private readonly ICloudConsoleService cloudConsole;

        public string Email { get; }
        public string ManagedBy { get; }

        public UserFlyoutViewModel(
            IAuthorization authorization,
            ICloudConsoleService cloudConsole)
        {
            this.cloudConsole = cloudConsole;
            this.Email = authorization.Email ?? string.Empty;

            //
            // Indicate if this is a managed (i.e Cloud Identity/Workspace) 
            // user account.
            //
            var hd = authorization.UserInfo?.HostedDomain;
            this.ManagedBy = (hd != null)
                ? $"(managed by {hd})"
                : string.Empty;
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void OpenMyAccountPage()
            => this.cloudConsole.OpenMyAccount();
    }
}
