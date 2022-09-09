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
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Authorization;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Commands;
using Google.Solutions.Testing.Common.Integration;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.Testing.Application.Views
{
    public partial class TestMainForm : Form, IMainForm, IAuthorizationSource, IJobService
    {
        public TestMainForm()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public IWin32Window Window => this;
        public DockPanel MainPanel => this.dockPanel;
        public ICommandContainer<IMainForm> ViewMenu => null;

        public void SetUrlHandler(IIapUrlHandler handler)
        {
            throw new NotImplementedException();
        }

        public ICommandContainer<TContext> AddMenu<TContext>(
            string caption,
            int? index,
            Func<TContext> queryCurrentContextFunc)
            where TContext : class
        {
            throw new NotImplementedException();
        }

        public void Minimize()
        {
        }

        //---------------------------------------------------------------------
        // IJobService.
        //---------------------------------------------------------------------

        public Task<T> RunInBackground<T>(JobDescription jobDescription, Func<CancellationToken, Task<T>> jobFunc)
        {
            // Run on UI thread to avoid multthreading issues in tests.
            var result = jobFunc(CancellationToken.None).Result;
            return Task.FromResult(result);
        }

        //---------------------------------------------------------------------
        // IAuthorizationSource.
        //---------------------------------------------------------------------

        private class SimpleAuthorization : IAuthorization
        {
            public ICredential Credential { get; }

            public string Email => "test@example.com";

            public UserInfo UserInfo => new UserInfo()
            {
                Email = "test@example.com"
            };

            public SimpleAuthorization(ICredential credential)
            {
                this.Credential = credential;
            }

            public Task ReauthorizeAsync(CancellationToken token)
            {
                throw new NotImplementedException();
            }

            public Task RevokeAsync()
            {
                throw new NotImplementedException();
            }

            public IDeviceEnrollment DeviceEnrollment => new SimpleDeviceEnrollment();
        }

        private class SimpleDeviceEnrollment : IDeviceEnrollment
        {
            public DeviceEnrollmentState State => DeviceEnrollmentState.Disabled;

            public X509Certificate2 Certificate => null;

            public Task RefreshAsync()
            {
                throw new NotImplementedException();
            }
        }

        public IAuthorization Authorization => new SimpleAuthorization(TestProject.GetAdminCredential());

        public static IDeviceEnrollment DeviceEnrollment => new SimpleDeviceEnrollment();

        public Task ReauthorizeAsync(CancellationToken token)
            => this.Authorization.ReauthorizeAsync(token);
    }
}
