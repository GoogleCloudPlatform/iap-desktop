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
using Google.Solutions.IapDesktop.Core.ProjectModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.App
{
    internal abstract class ConnectAppProtocolCommandBase : MenuCommandBase<IProjectModelNode>
    {
        private readonly IJobService jobService;
        private readonly INotifyDialog notifyDialog;

        protected ConnectAppProtocolCommandBase(
            string text,
            IJobService jobService,
            INotifyDialog notifyDialog)
            : base(text)
        {
            this.jobService = jobService.ExpectNotNull(nameof(jobService));
            this.notifyDialog = notifyDialog.ExpectNotNull(nameof(notifyDialog));
        }

        protected internal abstract Task<AppProtocolContext> CreateContextAsync(
            IProjectModelInstanceNode instance,
            CancellationToken cancellationToken);

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override bool IsAvailable(IProjectModelNode context)
        {
            return context is IProjectModelInstanceNode;
        }

        public override async Task ExecuteAsync(IProjectModelNode node)
        {
            var instance = (IProjectModelInstanceNode)node;

            var context = await CreateContextAsync(instance, CancellationToken.None)
                .ConfigureAwait(true);

            //
            // Connect a transport. This can take a bit, so do it in a job.
            //
            var transport = await this.jobService
                .RunAsync(
                    new JobDescription(
                        $"Connecting to {instance.Instance.Name}...",
                        JobUserFeedbackType.BackgroundFeedback),
                    cancellationToken => context.ConnectTransportAsync(cancellationToken))
                .ConfigureAwait(false);

            if (context.CanLaunchClient)
            {
                var process = context.LaunchClient(transport);
                process.Resume();

                //
                // Client app launched successfully. Keep the transport
                // open until the app terminates, but don't await.
                //
                // NB. Some executables launch a child process and terminate
                // immediately. To deal with such cases, we wait for all
                // processes in the job to finish.
                //
                Task processTerminatedTask = process.Job != null
                    ? process.Job.WaitForProcessesAsync(TimeSpan.MaxValue, CancellationToken.None)
                    : process.WaitAsync(TimeSpan.MaxValue, CancellationToken.None);

                _ = processTerminatedTask
                    .ContinueWith(_ =>
                    {
                        transport.Dispose();
                        process.Dispose();
                    });
            }
            else
            {
                //
                // There's no app to launch, just notify the user
                // about the forwarded port.
                //
                this.notifyDialog.ShowBalloon(
                    $"Port {transport.Endpoint.Port} forwarded to {transport.Target.Name}",
                    $"Use any client application and connect to {transport.Endpoint}");

                //
                // Prevent the GC from reclaiming the transport and closing
                // the underlying tunnels.
                //
                GC.SuppressFinalize(transport);
            }
        }
    }
}
