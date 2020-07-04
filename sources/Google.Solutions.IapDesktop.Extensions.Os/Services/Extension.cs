﻿//
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
using Google.Solutions.IapDesktop.Application.Services.Windows;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Os.Properties;
using Google.Solutions.IapDesktop.Extensions.Os.Services.InstanceDetails;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services
{
    /// <summary>
    /// Main class of the extension, instantiated on load.
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    public class Extension
    {
        public Extension(IServiceProvider serviceProvider)
        {
            var projectExplorer = serviceProvider.GetService<IProjectExplorer>();

            //
            // Add commands to project explorer.
            //

            projectExplorer.Commands.AddCommand(
                new Command<IProjectExplorerNode>(
                    "Show &details",
                    InstanceDetailsViewModel.GetCommandState,
                    context => serviceProvider.GetService<InstanceDetailsWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16
                },
                7);

            //
            // Add commands to main menu.
            //
            var mainForm = serviceProvider.GetService<IMainForm>();
            mainForm.ViewCommands.AddCommand(
                new Command<IMainForm>(
                    "&Instance details",
                    pseudoContext => CommandState.Enabled,
                    pseudoContext => serviceProvider.GetService<InstanceDetailsWindow>().ShowWindow())
                {
                    Image = Resources.ComputerDetails_16,
                    ShortcutKeys = Keys.Control | Keys.Alt | Keys.I
                },
                3);
        }
    }
}
