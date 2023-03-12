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
using Google.Solutions.Mvvm.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    public static partial class CommandBindingExtensions
    {
                private class ControlClickBinding : BindingExtensions.Binding
        {
            private readonly IBindingContext bindingContext;
            private IObservableCommand command;
            private readonly Control button;

            private async void OnClickAsync(object _, EventArgs __)
            {
                button.Enabled = false;
                try
                {
                    await this.command
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                                        if (button.FindForm() is Form form)
                    {
                        //
                        // Treat the successful command execution
                        // as dialog result.
                        //
                        if (form.AcceptButton == button)
                        {
                            form.DialogResult = DialogResult.OK;
                        }
                        else if (form.CancelButton == button)
                        {
                            form.DialogResult = DialogResult.Cancel;
                        }
                    }
                                    }
                catch (Exception e) when (e.IsCancellation())
                {
                    // Ignore.
                }
                catch (Exception e)
                {
                    this.bindingContext.OnCommandFailed(command, e);
                }
                finally
                {
                    button.Enabled = command.CanExecute.Value;
                }
            }

            public ControlClickBinding(
                Control button,
                IObservableCommand command,
                IBindingContext bindingContext)
            {
                this.button = button;
                this.command = command;
                this.bindingContext = bindingContext;

                button.Click += OnClickAsync;
            }

            public override void Dispose()
            {
                this.button.Click -= OnClickAsync;
            }
        }
                private class ToolStripButtonClickBinding : BindingExtensions.Binding
        {
            private readonly IBindingContext bindingContext;
            private IObservableCommand command;
            private readonly ToolStripButton button;

            private async void OnClickAsync(object _, EventArgs __)
            {
                button.Enabled = false;
                try
                {
                    await this.command
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(true);

                                    }
                catch (Exception e) when (e.IsCancellation())
                {
                    // Ignore.
                }
                catch (Exception e)
                {
                    this.bindingContext.OnCommandFailed(command, e);
                }
                finally
                {
                    button.Enabled = command.CanExecute.Value;
                }
            }

            public ToolStripButtonClickBinding(
                ToolStripButton button,
                IObservableCommand command,
                IBindingContext bindingContext)
            {
                this.button = button;
                this.command = command;
                this.bindingContext = bindingContext;

                button.Click += OnClickAsync;
            }

            public override void Dispose()
            {
                this.button.Click -= OnClickAsync;
            }
        }
            }
}
