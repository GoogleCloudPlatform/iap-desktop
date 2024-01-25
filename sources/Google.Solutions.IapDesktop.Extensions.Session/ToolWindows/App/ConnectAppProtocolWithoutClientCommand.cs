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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.App;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    /// <summary>
    /// Connect an AppProtocol that doesn't have an associated client.
    /// </summary>
    internal class ConnectAppProtocolWithoutClientCommand : ConnectAppProtocolCommandBase
    {
        private readonly AppProtocolContextFactory contextFactory;

        public ConnectAppProtocolWithoutClientCommand(
            IJobService jobService,
            AppProtocolContextFactory contextFactory,
            INotifyDialog notifyDialog) 
            : base(
                   $"&{contextFactory.Protocol.Name}", 
                   jobService, 
                   notifyDialog)
        {
            Debug.Assert(contextFactory.Protocol.Client == null);

            this.contextFactory = contextFactory.ExpectNotNull(nameof(contextFactory));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Id
        {
            get => $"{GetType().Name}.{this.contextFactory.Protocol.Name}";
        }

        protected override bool IsEnabled(IProjectModelNode context)
        {
            return this.contextFactory
                .Protocol
                .IsAvailable((IProjectModelInstanceNode)context);
        }

        protected internal override async Task<AppProtocolContext> CreateContextAsync(
            IProjectModelInstanceNode instance, 
            CancellationToken cancellationToken)
        {
            return (AppProtocolContext)await this.contextFactory
                .CreateContextAsync(
                    instance,
                    0,
                    cancellationToken)
                .ConfigureAwait(true);
        }
    }
}
