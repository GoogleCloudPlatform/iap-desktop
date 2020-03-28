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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Windows;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WindowTestFixtureBase : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        protected ServiceRegistry serviceRegistry;
        protected IServiceProvider serviceProvider;
        protected IMainForm mainForm;

        private IEventService eventService;
        private MockExceptionDialog exceptionDialog;

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

            var registry = new ServiceRegistry();
            registry.AddSingleton(new InventorySettingsRepository(hkcu.CreateSubKey(TestKeyPath)));
            registry.AddTransient<ProjectInventoryService>();

            var mainForm = new TestMainForm();
            registry.AddSingleton<IMainForm>(mainForm);
            registry.AddSingleton<IJobService>(mainForm);
            registry.AddSingleton<IAuthorizationService>(mainForm);

            this.eventService = new EventService(mainForm);
            registry.AddSingleton<IEventService>(this.eventService);

            this.exceptionDialog = new MockExceptionDialog();
            registry.AddSingleton<IExceptionDialog>(this.exceptionDialog);

            this.mainForm = mainForm;
            this.serviceRegistry = registry;
            this.serviceProvider = registry;

            mainForm.Show();

            PumpWindowMessages();
        }

        [TearDown]
        public void TearDown()
        {
            PumpWindowMessages();
            this.mainForm.Close();
        }

        protected void PumpWindowMessages()
            => System.Windows.Forms.Application.DoEvents();

        protected TEvent AwaitEvent<TEvent>() where TEvent : class
        {
            TEvent deliveredEvent = null;

            this.eventService.BindHandler<TEvent>(e =>
            {
                deliveredEvent = e;
            });

            while (deliveredEvent == null)
            {
                PumpWindowMessages();
            }

            return deliveredEvent;
        }

        protected static Instance CreateInstance(string instanceName, string zone, bool windows)
        {
            return new Instance()
            {
                Id = 1,
                Name = instanceName,
                Zone = "projects/-/zones/" + zone,
                MachineType = "zones/-/machineTypes/n1-standard-1",
                Disks = new[] {
                        new AttachedDisk()
                        {
                            GuestOsFeatures = new []
                            {
                                new GuestOsFeature()
                                {
                                    Type = windows ? "WINDOWS" : "WHATEVER"
                                }
                            }
                        }
                    }
            };
        }

    }

    internal static class ControlTestExtensions
    {
        public static T GetChild<T>(this Control control, string name) where T : Control
        {
            if (control.Controls.ContainsKey(name))
            {
                return (T)control.Controls[name];
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Control {control.Name} does not have a child control named {name}");
            }
        }
    }
}
