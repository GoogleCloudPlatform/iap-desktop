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
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views
{
    /// <summary>
    /// Default binding context for all views.
    /// </summary>
    public class ViewBindingContext : IBindingContext
    {
        private readonly IServiceProvider serviceProvider;

        public ViewBindingContext(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider; //TODO: .ThrowIfNull(nameof(serviceProvider));
        }

        public static ViewBindingContext CreateDummy() // TODO: REMOVE THIS REMOVE THIS
        {
            return new ViewBindingContext(null);
        }

        //---------------------------------------------------------------------
        // IBindingContext.
        //---------------------------------------------------------------------

        public void OnBindingCreated(IComponent control, IDisposable binding)
        {
            Debug.WriteLine($"Bining added for {control.GetType().Name} ({control})");
            control.Disposed += (_, __) => binding.Dispose();
        }

        public void OnCommandFailed(IComponent control, ICommand command, Exception exception)
        {
            this.serviceProvider.GetService<IExceptionDialog>().Show(
                this.serviceProvider.GetService<IMainWindow>(), 
                command.ActivityText, 
                exception);
        }

        public void OnBindingFailed(IComponent control, Exception exception)
        {
            this.serviceProvider.GetService<IExceptionDialog>().Show(
                this.serviceProvider.GetService<IMainWindow>(),
                "The control/view model binding failed",
                exception);
        }
    }
}
