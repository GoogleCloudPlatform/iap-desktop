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
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Extensions.Session.Controls;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Rdp;
using Google.Solutions.IapDesktop.Extensions.Session.Test.ToolWindows.Rdp;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test.Controls
{
    [TestFixture]
    [UsesCloudResources]
    [RequiresRdp]
    [Apartment(ApartmentState.STA)]
    public class TestRdpClientWithGroupPolicies
    {
        private async Task<RdpCredential> GenerateRdpCredentialAsync(
            InstanceLocator instance)
        {
            var username = "test" + Guid.NewGuid().ToString().Substring(0, 4);
            var credentialAdapter = new WindowsCredentialGenerator(
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    TestProject.AdminAuthorization,
                    TestProject.UserAgent));

            var credential = await credentialAdapter
                .CreateWindowsCredentialsAsync(
                    instance,
                    username,
                    UserFlags.AddToAdministrators,
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None)
                .ConfigureAwait(true);

            return new RdpCredential(
                credential.UserName,
                credential.Domain,
                credential.SecurePassword);
        }

        private async Task ConnectionSucceeds(
            ResourceTask<InstanceLocator> instanceTask,
            ResourceTask<IAuthorization> authTask)
        {
            var instance = await instanceTask;
            var auth = await authTask;

            using (var window = new RdpDiagnosticsWindow())
            using (var tunnel = IapTransport.ForRdp(instance, auth))
            {
                var rdpCredential = await
                    GenerateRdpCredentialAsync(instance)
                    .ConfigureAwait(true);

                window.Client.MainWindow = window;
                window.Client.Username = rdpCredential.User;
                window.Client.Password = rdpCredential.Password.AsClearText();
                window.Client.Server = "localhost";
                window.Client.ServerPort = (ushort)tunnel.Tunnel.LocalEndpoint.Port;

                window.Client.Connect();

                await window.Client
                    .AwaitStateAsync(RdpClient.ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                window.Close();
            }
        }

        [WindowsFormsTest]
        public async Task WhenNlaDisabledAndServerRequiresNla_ThenErrorIsShownAndWindowIsClosed(
            [WindowsInstance] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            var instance = await instanceTask;
            var auth = await authTask;

            using (var window = new RdpDiagnosticsWindow())
            using (var tunnel = IapTransport.ForRdp(instance, auth))
            {
                var rdpCredential = await 
                    GenerateRdpCredentialAsync(instance)
                    .ConfigureAwait(true);

                window.Client.MainWindow = window;
                window.Client.Username = $@".\{rdpCredential.User}";
                window.Client.Password = rdpCredential.Password.AsClearText();
                window.Client.Server = "localhost";
                window.Client.ServerPort = (ushort)tunnel.Tunnel.LocalEndpoint.Port;

                window.Client.EnableNetworkLevelAuthentication = false;
                window.Client.Connect();

                var eventArgs = await EventAssert.AssertRaisesEventAsync<ExceptionEventArgs>(
                    cb => window.Client.ConnectionFailed += (s, e) => cb(e))
                    .ConfigureAwait(true);

                Assert.IsInstanceOf<RdpDisconnectedException>(eventArgs.Exception);
                Assert.AreEqual(2825, ((RdpDisconnectedException)eventArgs.Exception).DisconnectReason);
            }
        }

        [WindowsFormsTest]
        public Task WhenMinEncryptionLevelSetToLow_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenMinEncryptionLevelSetToHigh_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v MinEncryptionLevel /d 3 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenSecurityLayserSetToRdp_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenSecurityLayserSetToNegotiate_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenSecurityLayserSetToSsl_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v SecurityLayer /d 2 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaEnabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenRequireUserAuthenticationForRemoteConnectionsByNlaDisabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v UserAuthentication /d 0 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }

        [WindowsFormsTest]
        public Task WhenLocalResourceRedirectionDisabled_ThenConnectionSucceeds(
            [WindowsInstance(InitializeScript = @"
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableClip /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableLPT /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCcm /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCdm /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fEnableSmartCard /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisablePNPRedir /d 1 /f | Out-Default
                & reg add ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"" /t REG_DWORD /v fDisableCpm /d 1 /f | Out-Default
            ")] ResourceTask<InstanceLocator> instanceTask,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<IAuthorization> authTask)
        {
            return ConnectionSucceeds(instanceTask, authTask);
        }
    }
}
