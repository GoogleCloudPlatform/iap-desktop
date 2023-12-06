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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.Testing.Application.Views
{
    public partial class TestMainForm : Form, IMainWindow, IJobService
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
        public ICommandContainer<IMainWindow> ViewMenu => null;

        public ICommandContainer<TContext> AddMenu<TContext>(
            string caption,
            int? index,
            Func<TContext> queryCurrentContextFunc)
            where TContext : class
        {
            return new CommandContainer<TContext>(
                ToolStripItemDisplayStyle.Text,
                new Mock<IContextSource<TContext>>().Object,
                new Mock<IBindingContext>().Object);
        }

        public bool IsWindowThread()
        {
            return !this.InvokeRequired;
        }

        public void Minimize()
        {
        }

        //---------------------------------------------------------------------
        // IJobService.
        //---------------------------------------------------------------------

        public Task<T> RunAsync<T>(
            JobDescription jobDescription, 
            Func<CancellationToken, Task<T>> jobFunc)
        {
            // Run on UI thread to avoid multthreading issues in tests.
            var result = jobFunc(CancellationToken.None).Result;
            return Task.FromResult(result);
        }

        public Task RunAsync(
            JobDescription jobDescription,
            Func<CancellationToken, Task> jobFunc)
        {
            return RunAsync<string>(
                jobDescription,
                async cancellationToken =>
                {
                    await jobFunc(cancellationToken).ConfigureAwait(true);
                    return string.Empty;
                });
        }
    }
}
