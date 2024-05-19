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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session
{
    public interface IRdpCredentialEditorFactory
    {
        /// <summary>
        /// Create an editor object for the connection settings.
        /// </summary>
        IRdpCredentialEditor Edit(Extensions.Session.Settings.ConnectionSettings settings);
    }

    [Service(typeof(IRdpCredentialEditorFactory))]
    public class RdpCredentialEditorFactory : IRdpCredentialEditorFactory
    {
        private readonly IServiceProvider serviceProvider;

        public RdpCredentialEditorFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IRdpCredentialEditor Edit(Extensions.Session.Settings.ConnectionSettings settings)
        {
            var theme = this.serviceProvider.GetService<IDialogTheme>();

            var newCredentialFactory = this.serviceProvider.GetViewFactory<NewCredentialsView, NewCredentialsViewModel>();
            newCredentialFactory.Theme = theme;

            var showCredentialFactory = this.serviceProvider.GetViewFactory<ShowCredentialsView, ShowCredentialsViewModel>();
            showCredentialFactory.Theme = theme;

            return new RdpCredentialEditor(
                this.serviceProvider.GetService<IWin32Window>(),
                settings,
                this.serviceProvider.GetService<IAuthorization>(),
                this.serviceProvider.GetService<IJobService>(),
                this.serviceProvider.GetService<IWindowsCredentialGenerator>(),
                this.serviceProvider.GetService<ITaskDialog>(),
                this.serviceProvider.GetService<ICredentialDialog>(),
                newCredentialFactory,
                showCredentialFactory);
        }
    }
}
