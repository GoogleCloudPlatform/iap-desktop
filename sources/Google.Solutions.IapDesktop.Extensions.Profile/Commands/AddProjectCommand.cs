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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Profile.Commands
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x100)]
    [Service]
    public class AddProjectCommand : ProfileMenuCommandBase
    {
        private readonly IProjectExplorer projectExplorer;

        public AddProjectCommand(IProjectExplorer projectExplorer)
            : base("&Add project...")
        {
            this.projectExplorer = projectExplorer.ExpectNotNull(nameof(projectExplorer));

            this.Image = Icons.AddProject_16;
        }

        public override Task ExecuteAsync(IUserProfile context)
        {
            return this.projectExplorer.ShowAddProjectDialogAsync();
        }
    }
}
