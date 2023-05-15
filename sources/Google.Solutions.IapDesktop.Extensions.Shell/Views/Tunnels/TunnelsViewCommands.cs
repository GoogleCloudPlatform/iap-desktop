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
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Extensions.Shell.Properties;
using Google.Solutions.Mvvm.Binding.Commands;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Tunnels
{
    [Service]
    public class TunnelsViewCommands
    {
        public TunnelsViewCommands(IToolWindowHost toolWindowHost)
        {
            this.WindowMenuOpen = new OpenToolWindowCommand
                <IMainWindow, TunnelsView, TunnelsViewModel>(
                    toolWindowHost,
                    "Active IAP &tunnels",
                    _ => true,
                    _ => true)
            {
                Image = Resources.Tunnel_16,
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.T
            };
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IMainWindow> WindowMenuOpen { get; }
    }
}
