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

using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Platform.Net;
using System;
using System.Drawing;
using System.Threading;

namespace Google.Solutions.IapDesktop.Application.Windows.Update
{
    public partial class ReleaseNotesView : DocumentWindow, IView<ReleaseNotesViewModel>
    {
        public ReleaseNotesView(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();

            this.document.Fonts.Text = new FontFamily("Segoe UI");
        }

        public void Bind(ReleaseNotesViewModel viewModel, IBindingContext context)
        {
            this.document.BindReadonlyObservableProperty(
                c => c.Markdown,
                viewModel,
                m => m.Summary,
                context);

            this.document.LinkClicked += (_, args)
                => Browser.Default.Navigate(args.LinkText);

            viewModel.RefreshCommand
                .ExecuteAsync(CancellationToken.None)
                .ContinueWith(_ => { });
        }
    }
}
