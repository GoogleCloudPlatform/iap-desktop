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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Settings;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Testing.Application.Views
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [Timeout(10 * 60 * 1000)]
    public class WindowTestFixtureBase : ApplicationFixtureBase
    {
        protected const string TestKeyPath = @"Software\Google\__Test";

        private MockExceptionDialog exceptionDialog;

        protected ServiceRegistry ServiceRegistry { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }
        protected IMainWindow MainWindow { get; private set; }
        protected IEventQueue EventService { get; private set; }

        protected Exception ExceptionShown => this.exceptionDialog.ExceptionShown;

        private class MockExceptionDialog : IExceptionDialog
        {
            public Exception ExceptionShown { get; private set; }

            public void Show(IWin32Window parent, string caption, Exception e)
            {
                this.ExceptionShown = e;
            }
        }

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var mainForm = new TestMainForm();
            this.EventService = new EventQueue(mainForm);

            var registry = new ServiceRegistry();
            registry.AddSingleton<IProjectRepository>(new ProjectRepository(
                hkcu.CreateSubKey(TestKeyPath)));
            registry.AddSingleton(new ToolWindowStateRepository(
                hkcu.CreateSubKey(TestKeyPath)));
            registry.AddSingleton<IRepository<IApplicationSettings>>(new ApplicationSettingsRepository(
                hkcu.CreateSubKey(TestKeyPath),
                null,
                null,
                UserProfile.SchemaVersion.Current));

            registry.AddSingleton<IMainWindow>(mainForm);
            registry.AddSingleton<IJobService>(mainForm);
            registry.AddSingleton<ISessionBroker, SessionBroker>();
            registry.AddSingleton<IEventQueue>(this.EventService);

            this.exceptionDialog = new MockExceptionDialog();
            registry.AddSingleton<IExceptionDialog>(this.exceptionDialog);

            this.MainWindow = mainForm;
            this.ServiceRegistry = registry;
            this.ServiceProvider = registry;

            mainForm.Show();

            PumpWindowMessages();
        }

        [TearDown]
        public void TearDown()
        {
            PumpWindowMessages();
            this.MainWindow.Close();
        }

        protected static void PumpWindowMessages()
            => System.Windows.Forms.Application.DoEvents();

        protected async Task<TEvent> AssertRaisesEventAsync<TEvent>(
            Func<Task> action,
            TimeSpan timeout) where TEvent : class
        {
            var deadline = DateTime.Now.Add(timeout);

            //
            // Set up event handler.
            //
            TEvent deliveredEvent = null;
            this.EventService.Subscribe<TEvent>(e =>
            {
                deliveredEvent = e;
            });

            //
            // Invoke the action - it can either synchrounously
            // or asynchronously deliver the event.
            //
            await action().ConfigureAwait(true);

            //
            // Wait for event in case it has not been delivered yet.
            //
            var lastLog = DateTime.Now;
            for (var i = 0; deliveredEvent == null; i++)
            {
                if (deadline < DateTime.Now)
                {
                    throw new TimeoutException(
                        $"Timeout waiting for event {typeof(TEvent).Name} elapsed");
                }

                //
                // Print out a message once per second.
                //
                if (DateTime.Now.Subtract(lastLog).TotalSeconds >= 1)
                {
                    Console.WriteLine($"Still waiting for {typeof(TEvent).Name} (until {deadline})");
                    lastLog = DateTime.Now;
                }

                //
                // Let the SynchronizationContext pump.
                //
                await Task.Yield();

                //
                // Let Windows pump.
                //
                PumpWindowMessages();
            }

            return deliveredEvent;
        }

        protected Task<TEvent> AssertRaisesEventAsync<TEvent>(
            Func<Task> action) where TEvent : class
            => AssertRaisesEventAsync<TEvent>(
                action,
                TimeSpan.FromSeconds(45));

        protected Task<TEvent> AssertRaisesEventAsync<TEvent>(
            Action action) where TEvent : class
            => AssertRaisesEventAsync<TEvent>(() =>
            {
                action();
                return Task.CompletedTask;
            });

        protected static void Delay(TimeSpan timeout)
        {
            var deadline = DateTime.Now.Add(timeout);

            while (DateTime.Now < deadline)
            {
                PumpWindowMessages();
            }
        }

        protected async Task<NetworkCredential> GenerateWindowsCredentialsAsync(InstanceLocator locator)
        {
            var username = "test" + Guid.NewGuid().ToString().Substring(0, 4);
            var credentialAdapter = new WindowsCredentialGenerator(
                new ComputeEngineClient(
                    ComputeEngineClient.CreateEndpoint(),
                    TestProject.AdminAuthorization,
                    TestProject.UserAgent));

            return await credentialAdapter
                .CreateWindowsCredentialsAsync(
                    locator,
                    username,
                    UserFlags.AddToAdministrators,
                    TimeSpan.FromSeconds(60),
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
    }
}
