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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Explorer.Windows.About;
using Google.Solutions.Mvvm.Binding;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Install
{
    [MenuCommand(typeof(HelpMenu), Rank = 0x1102)]
    [Service]
    public class AboutCommand : MenuCommandBase<IInstall>
    {
        private readonly IMainWindow mainWindow;
        private readonly WindowActivator<AboutView, AboutViewModel, IDialogTheme> windowFactory;

        public AboutCommand(
            IMainWindow mainWindow,
            WindowActivator<AboutView, AboutViewModel, IDialogTheme> windowFactory)
            : base("&About")
        {
            this.mainWindow = mainWindow;
            this.windowFactory = windowFactory;
        }

        protected override bool IsAvailable(IInstall _)
        {
            return true;
        }

        protected override bool IsEnabled(IInstall _)
        {
            return true;
        }

        public override void Execute(IInstall _)
        {
            using (var view = this.windowFactory.CreateDialog())
            {
                view.ShowDialog(this.mainWindow);
            }
        }
    }
}
