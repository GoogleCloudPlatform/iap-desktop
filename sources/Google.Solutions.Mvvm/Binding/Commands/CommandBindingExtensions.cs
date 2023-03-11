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
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    public static class CommandBindingExtensions
    {
        public static void BindCommand<TButton, TCommand, TModel>(
            this TButton button,
            TModel model,
            Func<TModel, TCommand> commandProperty,
            IBindingContext bindingContext)
            where TCommand : IObservableCommand
            where TButton : Control, IButtonControl
        {
            Precondition.ExpectNotNull(button, nameof(button));
            Precondition.ExpectNotNull(commandProperty, nameof(commandProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            var command = commandProperty(model);

            //
            // Apply initial values.
            //
            button.Enabled = command.CanExecute.Value;
            if (!string.IsNullOrEmpty(command.Text))
            {
                button.Text = command.Text;
            }

            //
            // Update control if command state changes.
            //
            var stateBinding = new BindingExtensions.NotifyObservablePropertyChangedBinding<bool>(
                command.CanExecute,
                canExecute => button.Enabled = canExecute);

            button.AttachDisposable(stateBinding);
            bindingContext.OnBindingCreated(button, stateBinding);

            //
            // Forward click events to the command.
            //
            async void OnClickAsync(object _, EventArgs __)
            {
                button.Enabled = false;
                try
                {
                    await command
                        .ExecuteAsync()
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
                catch (Exception e)
                {
                    bindingContext.OnCommandFailed(button, command, e);
                }
                finally
                {
                    button.Enabled = command.CanExecute.Value;
                }
            }

            var clickBinding = new CommandBindingHelpers.ControlClickBinding(button, OnClickAsync);

            button.AttachDisposable(clickBinding);
            bindingContext.OnBindingCreated(button, clickBinding);
        }

        public static void BindCommand< TCommand, TModel>(
            this ToolStripButton button,
            ToolStrip enclosingToolStrip,
            TModel model,
            Func<TModel, TCommand> commandProperty,
            IBindingContext bindingContext)
            where TCommand : IObservableCommand
        {
            Precondition.ExpectNotNull(button, nameof(button));
            Precondition.ExpectNotNull(enclosingToolStrip, nameof(enclosingToolStrip));
            Precondition.ExpectNotNull(commandProperty, nameof(commandProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            var command = commandProperty(model);

            //
            // Apply initial values.
            //
            button.Enabled = command.CanExecute.Value;
            if (!string.IsNullOrEmpty(command.Text))
            {
                button.Text = command.Text;
            }

            //
            // Update control if command state changes.
            //
            var stateBinding = new BindingExtensions.NotifyObservablePropertyChangedBinding<bool>(
                command.CanExecute,
                canExecute => button.Enabled = canExecute);

            button.AttachDisposable(stateBinding);
            bindingContext.OnBindingCreated(button, stateBinding);

            //
            // Forward click events to the command.
            //
            async void OnClickAsync(object _, EventArgs __)
            {
                button.Enabled = false;
                try
                {
                    await command
                        .ExecuteAsync()
                        .ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    bindingContext.OnCommandFailed(enclosingToolStrip, command, e);
                }
                finally
                {
                    button.Enabled = command.CanExecute.Value;
                }
            }

            var clickBinding = new CommandBindingHelpers.ToolStripButtonClickBinding(button, OnClickAsync);

            button.AttachDisposable(clickBinding);
            bindingContext.OnBindingCreated(button, clickBinding);
        }
    }
}
