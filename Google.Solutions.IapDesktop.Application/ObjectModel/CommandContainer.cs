//
// Copyright 2020 Google LLC
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
using Google.Solutions.IapDesktop.Application.Services.Windows;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    public class CommandContainer<TContext>
    {
        private TContext context;
        private readonly IWin32Window window;
        private readonly ToolStripItemCollection menuItems;
        private readonly CommandContainer<TContext> parent;
        private readonly IExceptionDialog exceptionDialog;

        internal CommandContainer(
            IWin32Window parent,
            ToolStripItemCollection menuItems,
            IExceptionDialog exceptionDialog)
            : this(parent, menuItems, exceptionDialog, null)
        {
        }

        internal CommandContainer(
            IWin32Window window,
            ToolStripItemCollection menuItems,
            IExceptionDialog exceptionDialog,
            CommandContainer<TContext> parent)
        {
            this.window = window;
            this.menuItems = menuItems;
            this.exceptionDialog = exceptionDialog;
            this.parent = parent;
        }

        public TContext Context 
        {
            get => this.context != null
                ? this.context
                : this.parent.Context;
            set
            {
                this.context = value;

                UpdateMenuItemState(this.menuItems, value);
            }
        }

        private static void UpdateMenuItemState(
            ToolStripItemCollection menuItems,
            TContext context)
        {
            // Update state of each menu item.
            foreach (var menuItem in menuItems
                .OfType<ToolStripMenuItem>()
                .Where(m => m.Tag is ICommand<TContext>))
            {
                switch (((ICommand<TContext>)menuItem.Tag).QueryState(context))
                {
                    case CommandState.Disabled:
                        menuItem.Visible = true;
                        menuItem.Enabled = false;
                        break;

                    case CommandState.Enabled:
                        menuItem.Enabled = menuItem.Visible = true;
                        break;

                    case CommandState.Unavailable:
                        menuItem.Visible = false;
                        break;
                }

                // NB. Only the top-most container has its context set.
                // Therefore, recursively update child menus as well.
                UpdateMenuItemState(menuItem.DropDownItems, context);
            }
        }

        public CommandContainer<TContext> AddCommand(
            string caption,
            System.Drawing.Image image,
            int? index,
            ICommand<TContext> command)
        {
            var menuItem = new ToolStripMenuItem(
                caption,
                image,
                (sender, args) =>
                {
                    if (this.Context is TContext context)
                    {
                        try
                        {
                            command.Execute(context);
                        }
                        catch (Exception e) when (e.IsCancellation())
                        {
                            // Ignore.
                        }
                        catch (Exception e)
                        {
                            this.exceptionDialog.Show(this.window, "Command failed", e);
                        }
                    }
                })
            {
                Tag = command
            };

            if (index.HasValue)
            {
                this.menuItems.Insert(index.Value, menuItem);
            }
            else
            {
                this.menuItems.Add(menuItem);
            }

            // Return a new contains that enables registering sub-commands.
            return new CommandContainer<TContext>(
                this.window,
                menuItem.DropDownItems,
                this.exceptionDialog,
                this);
        }
    }
}
