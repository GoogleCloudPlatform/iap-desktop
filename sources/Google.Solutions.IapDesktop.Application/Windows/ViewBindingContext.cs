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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows
{
    /// <summary>
    /// Default binding context for all views.
    /// </summary>
    public class ViewBindingContext : IBindingContext
    {
        private readonly IExceptionDialog exceptionDialog;
        private IWin32Window errorReportingOwner;

        /// <summary>
        /// Sets the current main window that can be used as parent
        /// for any error messages.
        /// 
        /// NB. During startup, we have a different main window than
        /// later.
        /// </summary>
        public void SetCurrentMainWindow(IWin32Window owner)
        {
            this.errorReportingOwner = owner;
        }

        public ViewBindingContext(IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog.ExpectNotNull(nameof(exceptionDialog));
        }

        //---------------------------------------------------------------------
        // IBindingContext.
        //---------------------------------------------------------------------

        public void OnBindingCreated(IComponent control, IDisposable binding)
        {
            Debug.Assert(this.errorReportingOwner != null);
            Debug.WriteLine($"Binding added for {control.GetType().Name} ({control})");
        }

        public void OnCommandFailed(ICommand command, Exception exception)
        {
            Debug.Assert(this.errorReportingOwner != null);
            this.exceptionDialog.Show(
                this.errorReportingOwner,
                $"{command.ActivityText} failed",
                exception);
        }
    }
}
