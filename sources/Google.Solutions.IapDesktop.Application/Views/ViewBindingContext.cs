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

using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views
{
    /// <summary>
    /// Default binding context for all views.
    /// </summary>
    public class ViewBindingContext : IBindingContext
    {
        private readonly IWin32Window window;
        private readonly IExceptionDialog exceptionDialog;

        public ViewBindingContext(
            IMainWindow mainWindow,
            IExceptionDialog exceptionDialog)
        {
            this.window = mainWindow.ThrowIfNull(nameof(mainWindow));
            this.exceptionDialog = exceptionDialog.ThrowIfNull(nameof(exceptionDialog));
        }

        //---------------------------------------------------------------------
        // IBindingContext.
        //---------------------------------------------------------------------

        public void OnBindingCreated(Control control, IDisposable binding)
        {
            Debug.WriteLine($"Bining added for {control.GetType().Name} ({control.Text})");
            control.Disposed += (_, __) => binding.Dispose();
        }

        public void OnCommandFailed(Control control, ICommand command, Exception exception)
        {
            this.exceptionDialog.Show(
                this.window, 
                command.ActivityText, 
                exception);
        }

        public void OnBindingFailed(Control control, Exception exception)
        {
            this.exceptionDialog.Show(
                this.window,
                "The control/view model binding failed",
                exception);
        }
    }
}
