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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Credentials
{
    public interface INewCredentialsDialog
    {
        GenerateCredentialsDialogResult ShowDialog(
            IWin32Window? owner,
            string suggestedUsername);
    }

    public class GenerateCredentialsDialogResult
    {
        public DialogResult Result { get; }
        public string Username { get; }

        public GenerateCredentialsDialogResult(
            DialogResult result,
            string username)
        {
            this.Result = result;
            this.Username = username;
        }
    }

    [Service(typeof(INewCredentialsDialog))]
    public class NewCredentialsDialog : INewCredentialsDialog
    {
        private readonly ViewFactory<NewCredentialsView, NewCredentialsViewModel> dialogFactory;

        public NewCredentialsDialog(IServiceProvider serviceProvider)
        {
            this.dialogFactory = serviceProvider
                .GetViewFactory<NewCredentialsView, NewCredentialsViewModel>();
            this.dialogFactory.Theme = serviceProvider.GetService<IThemeService>().DialogTheme;
        }

        public GenerateCredentialsDialogResult ShowDialog(
            IWin32Window? owner,
            string suggestedUsername)
        {
            using (var dialog = this.dialogFactory.CreateDialog())
            {
                dialog.ViewModel.Username = suggestedUsername;

                var result = dialog.ShowDialog(owner);

                return new GenerateCredentialsDialogResult(
                    result,
                    dialog.ViewModel.Username);
            }
        }
    }
}
