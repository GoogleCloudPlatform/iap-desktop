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

        public ViewBindingContext(IExceptionDialog exceptionDialog)
        {
            this.exceptionDialog = exceptionDialog.ExpectNotNull(nameof(exceptionDialog));
        }

        private IWin32Window GetMainWindow()
        {
            var mainWindowHwnd = Process.GetCurrentProcess().MainWindowHandle;
            return mainWindowHwnd == IntPtr.Zero ? null : new Win32Window(mainWindowHwnd);
        }

        //---------------------------------------------------------------------
        // IBindingContext.
        //---------------------------------------------------------------------

        public void OnBindingCreated(IComponent control, IDisposable binding)
        {
            Debug.WriteLine($"Binding added for {control.GetType().Name} ({control})");
        }

        public void OnCommandFailed(
            IWin32Window window,
            ICommandBase command,
            Exception exception)
        {
            //
            // NB. window might be null. If so, try to use the main
            // window as parent. This is typically the main form,
            // but could also be the authorize dialog.
            //
            var parent = window ?? GetMainWindow();

            ApplicationEventSource.Log.CommandFailed(
                command.Id,
                command.GetType().FullName(),
                exception.FullMessage());

            this.exceptionDialog.Show(
                parent,
                $"{command.ActivityText} failed",
                exception);
        }

        public void OnCommandExecuted(ICommandBase command)
        {
            if (command.Id != null)
            {
                ApplicationEventSource.Log.CommandExecuted(command.Id);
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Win32Window : IWin32Window
        {
            public Win32Window(IntPtr handle)
            {
                this.Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
