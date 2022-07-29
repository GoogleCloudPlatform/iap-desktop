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
using Google.Solutions.IapDesktop.Application.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Surface
{
    /// <summary>
    /// Command container that can be applied to one or more tool strips.
    /// </summary>
    public class ToolStripCommandContainer<TContext> : CommandContainer<TContext>
        where TContext : class
    {
        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public ToolStripCommandContainer(ToolStripItemDisplayStyle displayStyle)
            : base(displayStyle)
        {
        }

        public void ApplyTo(ToolStripItemCollection menu)
        {
            //
            // Populate eagerly.
            //
            menu.AddRange(this.MenuItems.ToArray());

            this.MenuItemsChanged += (s, e) =>
            {
                var oldMenuItemsWithCommand = menu
                    .Cast<ToolStripItem>()
                    .Where(i => i.Tag is ICommand<TContext>)
                    .ToList();

                foreach (var item in oldMenuItemsWithCommand)
                {
                    menu.Remove(item);
                }

                menu.AddRange(this.MenuItems.ToArray());
            };
        }

        public void ApplyTo(ToolStrip menu) => ApplyTo(menu.Items);
        public void ApplyTo(ToolStripMenuItem menu) => ApplyTo(menu.DropDownItems);
    }

    //public class MenuCommandSurface<TContext> : ICommandSurface<TContext>
    //    where TContext : class
    //{
    //    private readonly ICommandSurfaceContextSource<TContext> source;
    //    private readonly CommandContainer<TContext> commands;

    //    //---------------------------------------------------------------------
    //    // Publics.
    //    //---------------------------------------------------------------------

    //    public event EventHandler<ExceptionEventArgs> CommandFailed;

    //    public MenuCommandSurface(
    //        ToolStripItemDisplayStyle displayStyle,
    //        ICommandSurfaceContextSource<TContext> source)
    //    {
    //        this.source = source;
    //        this.commands = new CommandContainer<TContext>(
    //            displayStyle,
    //            e => this.CommandFailed?.Invoke(this, new ExceptionEventArgs(e)));
    //    }

    //    public void ApplyTo(ToolStripDropDown dropDownMenu)
    //    {
    //        //
    //        // Populate lazily when opened.
    //        //
    //        dropDownMenu.Opening += (s, a) =>
    //        {
    //            //
    //            // Query and set new context.
    //            //
    //            this.commands.Context = this.source.CurrentContext;

    //            //
    //            // Update menu items.
    //            //
    //            dropDownMenu.Items.Clear();
    //            dropDownMenu.Items.AddRange(this.commands.MenuItems.ToArray());
    //        };
    //    }

    //    //---------------------------------------------------------------------
    //    // ICommandSurface.
    //    //---------------------------------------------------------------------

    //    public ICommandContainer<TContext> Commands => this.commands;
    //}

    //public interface ICommandSurfaceContextSource<TContext>
    //{
    //    TContext CurrentContext { get; }
    //}
}
