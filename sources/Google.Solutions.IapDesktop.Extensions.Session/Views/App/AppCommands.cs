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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.Views.App
{
    [Service]
    public class AppCommands
    {
        private readonly IWin32Window ownerWindow;
        private readonly IJobService jobService;
        private readonly ProtocolRegistry protocolRegistry;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;
        private readonly IConnectionSettingsService settingsService;
        private readonly ICredentialDialog credentialDialog;

        public AppCommands(
            IWin32Window ownerWindow,
            IJobService jobService,
            ProtocolRegistry protocolRegistry,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService,
            ICredentialDialog credentialDialog)
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.protocolRegistry = protocolRegistry.ExpectNotNull(nameof(protocolRegistry));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));

            this.ConnectWithContextCommand = new ConnectWithAppCommand();
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ConnectWithContextCommand { get; }

        public IEnumerable<IContextCommand<IProjectModelNode>> ConnectWithAppCommands // TODO: Add tests
        {
            get
            {
                foreach (var protocol in this.protocolRegistry
                    .Protocols
                    .OfType<AppProtocol>())
                {
                    var factory = new AppContextFactory(
                        protocol,
                        this.transportFactory,
                        this.processFactory,
                        this.settingsService);

                    yield return new OpenWithAppCommand(
                        this.ownerWindow,
                        this.jobService,
                        factory,
                        this.credentialDialog);
                }
            }
        }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        private class ConnectWithAppCommand : MenuCommandBase<IProjectModelNode>
        {
            public ConnectWithAppCommand() 
                : base("Connect &with client")
            {
            }

            protected override bool IsAvailable(IProjectModelNode context)
            {
                return context is IProjectModelInstanceNode;
            }

            protected override bool IsEnabled(IProjectModelNode context)
            {
                return ((IProjectModelInstanceNode)context).IsRunning;
            }
        }
    }
}
