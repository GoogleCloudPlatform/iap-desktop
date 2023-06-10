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

using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    [Service]
    public class PackageInventoryCommands
    {
        public PackageInventoryCommands(IToolWindowHost toolWindowHost)
        {
            this.ContextMenuOpenInstalledPackages = new OpenToolWindowCommand
                <IProjectModelNode, InstalledPackageInventoryView, PackageInventoryViewModel>(
                    toolWindowHost,
                    "Show &installed packages",
                    context => context is IProjectModelInstanceNode ||
                        context is IProjectModelZoneNode ||
                        context is IProjectModelProjectNode,
                    _ => true)
            {
                Image = Resources.PackageInspect_16
            };
            this.ContextMenuOpenAvailablePackages = new OpenToolWindowCommand
                <IProjectModelNode, AvailablePackageInventoryView, PackageInventoryViewModel>(
                    toolWindowHost,
                    "Show &available updates",
                    context => context is IProjectModelInstanceNode ||
                        context is IProjectModelZoneNode ||
                        context is IProjectModelProjectNode,
                    _ => true)
            {
                Image = Resources.PackageUpdate_16
            };

            this.WindowMenuOpenInstalledPackages = new OpenToolWindowCommand
                <IMainWindow, InstalledPackageInventoryView, PackageInventoryViewModel>(
                    toolWindowHost,
                    "I&nstalled packages",
                    _ => true,
                    _ => true)
            {
                Image = Resources.PackageInspect_16,
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.P
            };
            this.WindowMenuOpenAvailablePackages = new OpenToolWindowCommand
                <IMainWindow, AvailablePackageInventoryView, PackageInventoryViewModel>(
                    toolWindowHost,
                    "&Available updates",
                    _ => true,
                    _ => true)
            {
                Image = Resources.PackageUpdate_16,
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.U
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuOpenInstalledPackages { get; }
        public IContextCommand<IProjectModelNode> ContextMenuOpenAvailablePackages { get; }

        public IContextCommand<IMainWindow> WindowMenuOpenInstalledPackages { get; }
        public IContextCommand<IMainWindow> WindowMenuOpenAvailablePackages { get; }
    }
}
