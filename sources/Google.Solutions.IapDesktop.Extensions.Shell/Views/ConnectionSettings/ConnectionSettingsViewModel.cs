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
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.ConnectionSettings
{
    internal class ConnectionSettingsViewModel : ViewModelBase, IPropertiesInspectorViewModel
    {
        internal const string DefaultWindowTitle = "Connection settings";

        private readonly IConnectionSettingsService settingsService;

        private bool isInformationBarVisible = false;
        private IPersistentSettingsCollection inspectedObject = null;
        private string windowTitle = DefaultWindowTitle;

        public string InformationText => "Changes only take effect after reconnecting";

        public ConnectionSettingsViewModel(IConnectionSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public bool IsInformationBarVisible
        {
            get => isInformationBarVisible;
            private set
            {
                this.isInformationBarVisible = value;
                RaisePropertyChange();
            }
        }

        public object InspectedObject
        {
            get => this.inspectedObject;
            private set
            {
                this.inspectedObject = (IPersistentSettingsCollection)value;
                RaisePropertyChange();
            }
        }

        public string WindowTitle
        {
            get => this.windowTitle;
            set
            {
                this.windowTitle = value;
                RaisePropertyChange();
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void SaveChanges()
        {
            Debug.Assert(this.inspectedObject != null);
            this.inspectedObject.Save();
        }

        public Task SwitchToModelAsync(IProjectExplorerNode node)
        {
            if (this.settingsService.IsConnectionSettingsAvailable(node))
            {
                this.IsInformationBarVisible =
                    node is IProjectExplorerInstanceNode &&
                    ((IProjectExplorerInstanceNode)node).IsConnected;

                this.InspectedObject = this.settingsService.GetConnectionSettings(node);
                this.WindowTitle = DefaultWindowTitle + $": {node.DisplayName}";
            }
            else
            {
                // Unsupported node.
                this.InspectedObject = null;
                this.IsInformationBarVisible = false;
                this.WindowTitle = DefaultWindowTitle;
            }

            return Task.CompletedTask;
        }
    }
}
