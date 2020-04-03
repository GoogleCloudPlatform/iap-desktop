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

using Google.Solutions.Compute;
using Google.Solutions.IapDesktop.Application.Adapters;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Windows;
using System;
using System.Linq;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Windows.SerialLog
{
    public class SerialLogService
    {
        private readonly DockPanel dockPanel;
        private readonly IExceptionDialog exceptionDialog;
        private readonly IServiceProvider serviceProvider;

        public SerialLogService(IServiceProvider serviceProvider)
        {
            this.dockPanel = serviceProvider.GetService<IMainForm>().MainPanel;
            this.exceptionDialog = serviceProvider.GetService<IExceptionDialog>();
            this.serviceProvider = serviceProvider;
        }

        private SerialLogWindow TryGetExistingWindow(VmInstanceReference vmInstance)
            => this.dockPanel.Contents
                .EnsureNotNull()
                .OfType<SerialLogWindow>()
                .Where(w => w.Instance == vmInstance)
                .FirstOrDefault();

        public void ShowSerialLog(VmInstanceReference vmInstance)
        {
            var window = TryGetExistingWindow(vmInstance);
            if (window == null)
            {
                var gceAdapter = this.serviceProvider.GetService<IComputeEngineAdapter>();

                window = new SerialLogWindow(vmInstance);
                window.TailSerialPortStream(gceAdapter.GetSerialPortOutput(vmInstance));
            }

            window.Show(this.dockPanel, DockState.DockBottomAutoHide);
            this.dockPanel.ActiveAutoHideContent = window;
            window.Activate();
        }
    }
}
