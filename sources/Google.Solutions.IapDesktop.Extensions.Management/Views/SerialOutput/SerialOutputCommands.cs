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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.Views.SerialOutput
{
    [Service]
    public class SerialOutputCommands
    {
        public SerialOutputCommands(IToolWindowHost toolWindowHost)
        {

            this.ContextMenuOpenCom1 = new OpenToolWindowCommand
                <IProjectModelNode, SerialOutputViewCom1, SerialOutputViewModel>(
                    toolWindowHost,
                    "Show serial port &output (COM1)",
                    context => context is IProjectModelInstanceNode,
                    context => context is IProjectModelInstanceNode vm && vm.IsRunning)
            {
                Image = Resources.Log_16
            };

            this.WindowMenuOpenCom1 = new OpenToolWindowCommand
                <IMainWindow, SerialOutputViewCom1, SerialOutputViewModel>(
                    toolWindowHost,
                    "COM&1 (log)",
                    _ => true,
                    _ => true)
            {
                Image = Resources.Log_16,
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.O
            };
            this.WindowMenuOpenCom3 = new OpenToolWindowCommand
                <IMainWindow, SerialOutputViewCom3, SerialOutputViewModel>(
                    toolWindowHost,
                    "COM&3 (setup log)",
                    _ => true,
                    _ => true)
            {
                Image = Resources.Log_16,
            };
            this.WindowMenuOpenCom4 = new OpenToolWindowCommand
                <IMainWindow, SerialOutputViewCom4, SerialOutputViewModel>(
                    toolWindowHost,
                    "COM&4 (agent)",
                    _ => true,
                    _ => true)
            {
                Image = Resources.Log_16,
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ContextMenuOpenCom1 { get; }

        public IContextCommand<IMainWindow> WindowMenuOpenCom1 { get; }
        public IContextCommand<IMainWindow> WindowMenuOpenCom3 { get; }
        public IContextCommand<IMainWindow> WindowMenuOpenCom4 { get; }
    }
}
