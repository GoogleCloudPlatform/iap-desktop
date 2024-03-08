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

using Google.Solutions.Settings;
using Google.Solutions.IapDesktop.Application.ToolWindows.Properties;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.ConnectionSettings
{
    [Service]
    public class ConnectionSettingsViewModel : ViewModelBase, IPropertiesInspectorViewModel
    {
        internal const string RequiresReconnectWarning = "Changes take effect after reconnecting";
        internal const string DefaultWindowTitle = "Connection settings";

        private readonly IConnectionSettingsService settingsService;
        private readonly ISessionBroker globalSessionBroker;

        public ConnectionSettingsViewModel(
            IConnectionSettingsService settingsService,
            ISessionBroker globalSessionBroker)
        {
            this.settingsService = settingsService;
            this.globalSessionBroker = globalSessionBroker;

            this.informationText = ObservableProperty.Build<string>(null);
            this.inspectedObject = ObservableProperty.Build<object>(null);
            this.windowTitle = ObservableProperty.Build(DefaultWindowTitle);
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        private readonly ObservableProperty<string> informationText;
        private readonly ObservableProperty<object> inspectedObject;
        private readonly ObservableProperty<string> windowTitle;

        public IObservableProperty<string> InformationText => this.informationText;
        public IObservableProperty<object> InspectedObject => this.inspectedObject;
        public IObservableProperty<string> WindowTitle => this.windowTitle;

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void SaveChanges()
        {
            Debug.Assert(this.inspectedObject != null);

            var settings = (IPersistentSettingsCollection)this.inspectedObject.Value;
            settings.Save();
        }

        public Task SwitchToModelAsync(IProjectModelNode node)
        {
            if (this.settingsService.IsConnectionSettingsAvailable(node))
            {
                var isConnected =
                    node is IProjectModelInstanceNode vmNode &&
                    this.globalSessionBroker.IsConnected(vmNode.Instance);

                this.informationText.Value = isConnected ? RequiresReconnectWarning : null;
                this.inspectedObject.Value = this.settingsService.GetConnectionSettings(node);
                this.windowTitle.Value = DefaultWindowTitle + $": {node.DisplayName}";
            }
            else
            {
                // Unsupported node.
                this.informationText.Value = null;
                this.inspectedObject.Value = null;
                this.windowTitle.Value = DefaultWindowTitle;
            }

            return Task.CompletedTask;
        }
    }
}
