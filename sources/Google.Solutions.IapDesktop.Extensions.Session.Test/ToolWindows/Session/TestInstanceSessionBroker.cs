////
//// Copyright 2023 Google LLC
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

//using Google.Apis.Auth.OAuth2;
//using Google.Solutions.Apis.Auth;
//using Google.Solutions.Apis.Locator;
//using Google.Solutions.IapDesktop.Application.Theme;
//using Google.Solutions.IapDesktop.Application.Windows;
//using Google.Solutions.IapDesktop.Core.ObjectModel;
//using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
//using Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Rdp;
//using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Rdp;
//using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Session;
//using Google.Solutions.Mvvm.Binding;
//using Google.Solutions.Testing.Apis.Integration;
//using Google.Solutions.Testing.Apis.Mocks;
//using Google.Solutions.Testing.Application.ObjectModel;
//using Google.Solutions.Testing.Application.Views;
//using NUnit.Framework;
//using System;
//using System.Threading.Tasks;

//namespace Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Session
//{
//    [TestFixture]
//    [UsesCloudResources]
//    public class TestInstanceSessionBroker : WindowTestFixtureBase
//    {
//        //
//        // Use a larger machine type as all this RDP'ing consumes a fair
//        // amount of memory.
//        //
//        private const string MachineTypeForRdp = "n1-highmem-2";

//        private IServiceProvider CreateServiceProvider(IAuthorization authorization)
//        {
//            var registry = new ServiceRegistry(this.ServiceRegistry);
//            registry.AddTransient<RdpView>();
//            registry.AddTransient<RdpViewModel>();
//            registry.AddMock<IThemeService>();
//            registry.AddMock<IBindingContext>();
//            registry.AddTransient<IToolWindowHost, ToolWindowHost>();
//            registry.AddSingleton(authorization);

//            return registry;
//        }

//        //---------------------------------------------------------------------
//        // IsConnected.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenNotConnected_ThenIsConnectedIsFalse()
//        {
//            var serviceProvider = CreateServiceProvider(
//                TestProject.InvalidAuthorization);

//            var sampleLocator = new InstanceLocator("project", "zone", "instance");
//            var broker = new InstanceSessionBroker(serviceProvider);
//            Assert.IsFalse(broker.IsConnected(sampleLocator));
//        }

//        //---------------------------------------------------------------------
//        // ActiveSession.
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenNotConnected_ThenActiveSessionReturnsNull()
//        {
//            var serviceProvider = CreateServiceProvider(
//                TestProject.InvalidAuthorization);

//            var broker = new InstanceSessionBroker(serviceProvider);
//            Assert.IsNull(broker.ActiveSession);
//        }

//        //---------------------------------------------------------------------
//        // TryActivate
//        //---------------------------------------------------------------------

//        [Test]
//        public void WhenNotConnected_ThenTryActivateReturnsFalse()
//        {
//            var serviceProvider = CreateServiceProvider(
//                TestProject.InvalidAuthorization);

//            var sampleLocator = new InstanceLocator("project", "zone", "instance");
//            var broker = new InstanceSessionBroker(serviceProvider);
//            Assert.IsFalse(broker.TryActivate(sampleLocator, out var _));

//        }

//        [Test]
//        [RequiresRdp]
//        public async Task WhenRdpSessionExists_ThenTryActivateSucceeds(
//            [WindowsInstance(MachineType = MachineTypeForRdp)] ResourceTask<InstanceLocator> testInstance,
//            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> auth)
//        {
//            var serviceProvider = CreateServiceProvider(await auth);
//            var instance = await testInstance;

//            using (var tunnel = IapTransport.ForRdp(
//                instance,
//                await auth))
//            {
//                var credentials = await GenerateWindowsCredentialsAsync(instance).ConfigureAwait(true);
//                var rdpCredential = new RdpCredential(
//                    credentials.UserName,
//                    credentials.Domain,
//                    credentials.SecurePassword);

//                var rdpParameters = new RdpParameters();

//                // Connect
//                var broker = new InstanceSessionBroker(serviceProvider);
//                IRdpSession session = null;
//                await AssertRaisesEventAsync<SessionStartedEvent>(
//                        () => session = (RdpView)broker.ConnectRdpSession(
//                            instance,
//                            tunnel,
//                            rdpParameters,
//                            rdpCredential))
//                    .ConfigureAwait(true);

//                Assert.IsNull(this.ExceptionShown);

//                Assert.AreSame(session, RdpView.TryGetActivePane(this.MainWindow));
//                Assert.AreSame(session, RdpView.TryGetExistingPane(this.MainWindow, instance));
//                Assert.IsTrue(broker.IsConnected(instance));
//                Assert.IsTrue(broker.TryActivate(instance, out var _));

//                await AssertRaisesEventAsync<SessionEndedEvent>(
//                        () => session.Close())
//                    .ConfigureAwait(true);
//            }
//        }
//    }
//}
//TODO: redo tests