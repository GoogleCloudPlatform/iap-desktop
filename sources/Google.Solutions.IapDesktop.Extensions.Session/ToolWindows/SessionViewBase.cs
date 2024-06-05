//
// Copyright 2022 Google LLC
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
using Google.Solutions.IapDesktop.Application.Profile.Settings;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Binding.Commands;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows
{
    /// <summary>
    /// Base class for sessions.
    /// </summary>
    public class SessionViewBase : DocumentWindow
    {
        private readonly IBindingContext bindingContext;
        private ICommandContainer<ISession>? contextCommands;

        protected SessionViewBase(IBindingContext bindingContext)
        {
            // Constructor is for testing only.
            this.bindingContext = bindingContext;
        }

        protected SessionViewBase(
            IMainWindow mainWindow,
            ToolWindowStateRepository stateRepository,
            IBindingContext bindingContext)
            : base(mainWindow, stateRepository)
        {
            this.bindingContext = bindingContext.ExpectNotNull(nameof(bindingContext));
        }

        public void ActivateSession()
        {
            SwitchToDocument();
        }

        //---------------------------------------------------------------------
        // Context menu.
        //---------------------------------------------------------------------

        public ICommandContainer<ISession>? ContextCommands
        {
            get => this.contextCommands;
            set
            {
                if (this.contextCommands != null)
                {
                    //
                    // Don't allow binding multiple command containers to
                    // the same menu (or binding the same container multiple
                    // times) as that leads to duplication.
                    //
                    throw new InvalidOperationException(
                        "Context commands have already been set");
                }

                if (this.TabPageContextMenuStrip == null)
                {
                    //
                    // There's a rare chance that the context menu is
                    // null because the window is being closed. In that
                    // case, do nothing.
                    //
                }
                else
                {
                    this.contextCommands = value.ExpectNotNull(nameof(value));
                    this.contextCommands.BindTo(
                        this.TabPageContextMenuStrip,
                        this.bindingContext);
                }

                //
                // Hide the Close menu item since it's most
                // likely redundant now.
                //
                this.ShowCloseMenuItemInContextMenu = false;
            }
        }
    }
}
