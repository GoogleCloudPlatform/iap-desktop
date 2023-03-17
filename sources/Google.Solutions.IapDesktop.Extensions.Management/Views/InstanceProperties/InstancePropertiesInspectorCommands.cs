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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.InstanceProperties
{
    [Service]
    public class InstancePropertiesInspectorCommands
    {
        public InstancePropertiesInspectorCommands(IServiceProvider serviceProvider)
        {
            this.ContextMenuOpen = new OpenToolWindowCommand
                <IProjectModelNode, InstancePropertiesInspectorView, InstancePropertiesInspectorViewModel>(
                    serviceProvider,
                    "P&roperties",
                    context => context is IProjectModelInstanceNode,
                    _ => true)
            {
                Image = Resources.ComputerDetails_16,
                ShortcutKeys = Keys.Alt | Keys.Enter
            };

            this.ToolbarOpen = new OpenToolWindowCommand
                <IProjectModelNode, InstancePropertiesInspectorView, InstancePropertiesInspectorViewModel>(
                    serviceProvider,
                    "P&roperties",
                    _ => true,
                    context => context is IProjectModelInstanceNode)
            {
                Image = Resources.ComputerDetails_16
            };

            this.WindowMenuOpen = new OpenToolWindowCommand
                <IMainWindow, InstancePropertiesInspectorView, InstancePropertiesInspectorViewModel>(
                    serviceProvider,
                    "&Instance properties",
                    _ => true,
                    _ => true)
            {
                Image = Resources.ComputerDetails_16,
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.I
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuOpen { get; }
        public IContextCommand<IProjectModelNode> ToolbarOpen { get; }
        public IContextCommand<IMainWindow> WindowMenuOpen { get; }
    }
}
