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
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
   

    public class CommandContainer<TContext>
    {
        private readonly IWin32Window parent;
        private readonly ToolStripItemCollection menuItems;
        private readonly Func<TContext> captureContextFunc;
        private readonly IExceptionDialog exceptionDialog;

        internal CommandContainer(
            IWin32Window parent,
            ToolStripItemCollection menuItems,
            Func<TContext> captureContextFunc,
            IExceptionDialog exceptionDialog)
        {
            this.parent = parent;
            this.menuItems = menuItems;
            this.captureContextFunc = captureContextFunc;
            this.exceptionDialog = exceptionDialog;
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
                    if (this.captureContextFunc() is TContext context)
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
                            this.exceptionDialog.Show(this.parent, "Command failed", e);
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
                this.parent,
                menuItem.DropDownItems,
                this.captureContextFunc,
                this.exceptionDialog);
        }
    }
}
