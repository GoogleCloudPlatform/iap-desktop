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
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Platform.Dispatch;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
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
        private readonly IInputDialog inputDialog;
        private readonly INotifyDialog notifyDialog;

        public AppCommands(
            IWin32Window ownerWindow,
            IJobService jobService,
            ProtocolRegistry protocolRegistry,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService,
            ICredentialDialog credentialDialog,
            IInputDialog inputDialog,
            INotifyDialog notifyDialog)
        {
            this.ownerWindow = ownerWindow.ExpectNotNull(nameof(ownerWindow));
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.protocolRegistry = protocolRegistry.ExpectNotNull(nameof(protocolRegistry));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
            this.credentialDialog = credentialDialog.ExpectNotNull(nameof(credentialDialog));
            this.inputDialog = inputDialog.ExpectNotNull(nameof(inputDialog));
            this.notifyDialog = notifyDialog.ExpectNotNull(nameof(notifyDialog));

            this.ConnectWithContextCommand = new ConnectWithAppCommand();
        }

        //---------------------------------------------------------------------
        // Context commands.
        //---------------------------------------------------------------------

        public IContextCommand<IProjectModelNode> ConnectWithContextCommand { get; }

        public IEnumerable<IContextCommand<IProjectModelNode>> ConnectWithAppCommands
        {
            get
            {
                foreach (var protocol in this.protocolRegistry
                    .Protocols
                    .OfType<AppProtocol>()
                    .OrderBy(p => p.Name))
                {
                    var factory = new AppProtocolContextFactory(
                        protocol,
                        this.transportFactory,
                        this.processFactory,
                        this.settingsService);

                    if (protocol.Client != null)
                    {
                        yield return new ConnectAppProtocolWithClientCommand(
                            this.ownerWindow,
                            this.jobService,
                            factory,
                            this.credentialDialog,
                            this.notifyDialog,
                            false);

                        if (protocol.Client.IsNetworkLevelAuthenticationSupported)
                        {
                            //
                            // Add anther "as user..." command.
                            //
                            yield return new ConnectAppProtocolWithClientCommand(
                                this.ownerWindow,
                                this.jobService,
                                factory,
                                this.credentialDialog,
                                this.notifyDialog,
                                true);
                        }
                    }
                    else
                    {
                        yield return new ConnectAppProtocolWithoutClientCommand(
                           this.jobService,
                           factory,
                           this.notifyDialog);
                    }
                }

                yield return new ForwardLocalPortCommand(
                    this.ownerWindow,
                    "Forward &local port...",
                    this.transportFactory,
                    this.processFactory,
                    this.jobService,
                    this.inputDialog,
                    this.notifyDialog);
            }
        }

        //---------------------------------------------------------------------
        // Command classes.
        //---------------------------------------------------------------------

        private class ConnectWithAppCommand : MenuCommandBase<IProjectModelNode>
        {
            public ConnectWithAppCommand()
                : base("Connect client a&pplication")
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
