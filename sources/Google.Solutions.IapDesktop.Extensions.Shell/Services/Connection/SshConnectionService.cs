////
//// Copyright 2020 Google LLC
////
//// Licensed to the Apache Software Foundation (ASF) under one
//// or more contributor license agreements.  See the NOTICE file
//// distributed with this work for additional information
//// regarding copyright ownership.  The ASF licenses this file
//// to you under the Apache License, Version 2.0 (the
//// "License"); you may not use this file except in compliance
//// with the License.  You may obtain a copy of the License at
//// 
////   http://www.apache.org/licenses/LICENSE-2.0
//// 
//// Unless required by applicable law or agreed to in writing,
//// software distributed under the License is distributed on an
//// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//// KIND, either express or implied.  See the License for the
//// specific language governing permissions and limitations
//// under the License.
////

//using Google.Solutions.Common.Text;
//using Google.Solutions.Common.Util;
//using Google.Solutions.IapDesktop.Application.ObjectModel;
//using Google.Solutions.IapDesktop.Application.Services.Auth;
//using Google.Solutions.IapDesktop.Application.Services.Integration;
//using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
//using Google.Solutions.IapDesktop.Application.Views;
//using Google.Solutions.IapDesktop.Extensions.Shell.Data;
//using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
//using Google.Solutions.IapDesktop.Extensions.Shell.Services.ConnectionSettings;
//using Google.Solutions.IapDesktop.Extensions.Shell.Services.Settings;
//using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
//using Google.Solutions.IapDesktop.Extensions.Shell.Services.Tunnel;
//using System;
//using System.Diagnostics;
//using System.Globalization;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
//{
//    public interface ISshConnectionService
//    {
//        Task<ConnectionTemplate<SshSessionParameters>> PrepareConnectionAsync(
//            IProjectModelInstanceNode vmNode);
//    }

//    [Service(typeof(ISshConnectionService))]
//    public class SshConnectionService : ConnectionServiceBase, ISshConnectionService
//    {
//        private readonly IWin32Window window;
//        private readonly IJobService jobService;
//        private readonly IConnectionSettingsService settingsService;
//        private readonly IKeyAuthorizationService authorizedKeyService;
//        private readonly IKeyStoreAdapter keyStoreAdapter;
//        private readonly IAuthorization authorization;
//        private readonly SshSettingsRepository sshSettingsRepository;
//        private readonly IProjectModelService projectModelService;

//        public SshConnectionService(
//            IMainWindow window,
//            IAuthorization authorization,
//            IProjectModelService projectModelService,
//            ITunnelBrokerService tunnelBroker,
//            IConnectionSettingsService settingsService,
//            IKeyAuthorizationService authorizedKeyService,
//            IKeyStoreAdapter keyStoreAdapter,
//            SshSettingsRepository sshSettingsRepository,
//            IJobService jobService)
//            : base(jobService, tunnelBroker)
//        {
//            this.window = window.ExpectNotNull(nameof(window));
//            this.authorization = authorization.ExpectNotNull(nameof(authorization));
//            this.projectModelService = projectModelService.ExpectNotNull(nameof(projectModelService));
//            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));
//            this.authorizedKeyService = authorizedKeyService.ExpectNotNull(nameof(authorizedKeyService));
//            this.keyStoreAdapter = keyStoreAdapter.ExpectNotNull(nameof(keyStoreAdapter));
//            this.sshSettingsRepository = sshSettingsRepository.ExpectNotNull(nameof(sshSettingsRepository));
//            this.jobService = jobService.ExpectNotNull(nameof(jobService));
//        }

//        //---------------------------------------------------------------------
//        // ISshConnectionService.
//        //---------------------------------------------------------------------

//        public async Task<ConnectionTemplate<SshSessionParameters>> PrepareConnectionAsync(
//            IProjectModelInstanceNode vmNode)
//        {
//            Debug.Assert(vmNode.IsSshSupported());

//            //
//            // Select node so that tracking windows are updated.
//            //
//            await this.projectModelService.SetActiveNodeAsync(
//                    vmNode,
//                    CancellationToken.None)
//                .ConfigureAwait(true);

//            var instance = vmNode.Instance;
//            var settings = (InstanceConnectionSettings)this.settingsService
//                .GetConnectionSettings(vmNode)
//                .TypedCollection;
//            var timeout = TimeSpan.FromSeconds(settings.SshConnectionTimeout.IntValue);

//            //
//            // Start job to create IAP tunnel.
//            //
//            var tunnelTask = PrepareTransportAsync(
//                vmNode.Instance,
//                (ushort)settings.SshPort.IntValue,
//                timeout);

//            //
//            // Load persistent CNG key. This must be done on the UI thread.
//            //
//            var sshSettings = this.sshSettingsRepository.GetSettings();
//            var sshKey = this.keyStoreAdapter.OpenSshKeyPair(
//                sshSettings.PublicKeyType.EnumValue,
//                this.authorization,
//                true,
//                this.window);
//            Debug.Assert(sshKey != null);

//            //
//            // Start job to publish key, using whatever mechanism is appropriate
//            // for this instance.
//            //

//            try
//            {
//                var authorizedKeyTask = this.jobService.RunInBackground(
//                    new JobDescription(
//                        $"Publishing SSH key for {instance.Name}...",
//                        JobUserFeedbackType.BackgroundFeedback),
//                    async token =>
//                    {
//                        //
//                        // Authorize the key.
//                        //
//                        return await this.authorizedKeyService.AuthorizeKeyAsync(
//                                vmNode.Instance,
//                                sshKey,
//                                TimeSpan.FromSeconds(sshSettings.PublicKeyValidity.IntValue),
//                                settings.SshUsername.StringValue.NullIfEmpty(),
//                                KeyAuthorizationMethods.All,
//                                token)
//                            .ConfigureAwait(true);
//                    });

//                //
//                // Wait for both jobs to continue (they are both fairly slow).
//                //

//                await Task.WhenAll(tunnelTask, authorizedKeyTask)
//                    .ConfigureAwait(true);

//                var language = sshSettings.IsPropagateLocaleEnabled.BoolValue
//                     ? CultureInfo.CurrentUICulture
//                     : null;

//                //
//                // NB. The template takes ownership of the key and will retain
//                // it for the lifetime of the session.
//                //
//                return new ConnectionTemplate<SshSessionParameters>(
//                    tunnelTask.Result,
//                    new SshSessionParameters(
//                        authorizedKeyTask.Result,
//                        language,
//                        timeout));
//            }
//            catch (Exception)
//            {
//                sshKey.Dispose();
//                throw;
//            }
//        }
//    }
//}
//TODO: remove