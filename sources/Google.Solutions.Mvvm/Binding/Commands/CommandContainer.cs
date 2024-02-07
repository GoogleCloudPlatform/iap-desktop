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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Set of commands.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface ICommandContainer<TContext> : IDisposable
        where TContext : class
    {
        ICommandContainer<TContext> AddCommand(
            IContextCommand<TContext> command);

        ICommandContainer<TContext> AddCommand(
            IContextCommand<TContext> command,
            int? index);

        void AddSeparator(int? index = null);

        void ExecuteCommandByKey(Keys keys);

        void ExecuteDefaultCommand();

        void BindTo(
            ToolStripDropDownMenu menu,
            IBindingContext bindingContext);

        void BindTo(
            ToolStripMenuItem menu,
            IBindingContext bindingContext);
    }

    public sealed class CommandContainer<TContext> : ICommandContainer<TContext>, IDisposable
        where TContext : class
    {
        private readonly ToolStripItemDisplayStyle displayStyle;
        private readonly ObservableCollection<MenuItemViewModelBase> menuItems;
        private readonly IBindingContext bindingContext;

        internal IContextSource<TContext> ContextSource { get; }

        internal ObservableCollection<MenuItemViewModelBase> MenuItems => this.menuItems;

        private CommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            IContextSource<TContext> contextSource,
            ObservableCollection<MenuItemViewModelBase> items,
            IBindingContext bindingContext)
        {
            this.displayStyle = displayStyle;
            this.menuItems = items;
            this.ContextSource = contextSource;
            this.bindingContext = bindingContext;
        }

        public CommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            IContextSource<TContext> contextSource,
            IBindingContext bindingContext)
            : this(
                  displayStyle,
                  contextSource,
                  new ObservableCollection<MenuItemViewModelBase>(),
                  bindingContext)
        {
            if (this.ContextSource is INotifyPropertyChanged observable)
            {
                observable.OnPropertyChange(
                    s => ((IContextSource<TContext>)s).Context,
                    context =>
                    {
                        MenuItemViewModel.OnContextUpdated(this.menuItems);
                    },
                    bindingContext);
            }
        }

        public void BindTo(
            ToolStripItemCollection view,
            IBindingContext bindingContext)
        {
            view.BindCollection(
                this.menuItems,
                m => m is SeparatorViewModel,
                m => m.Text,
                m => m.ToolTip,
                m => m.Image,
                m => m.ShortcutKeys,
                m => m.IsVisible,
                m => m.IsEnabled,
                m => m.DisplayStyle,
                m => m.Children,
                m => m.Invoke(),
                bindingContext);
        }

        public void ForceRefresh()
        {
            MenuItemViewModel.OnContextUpdated(this.menuItems);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
        }

        //---------------------------------------------------------------------
        // ICommandContainer.
        //---------------------------------------------------------------------

        public void BindTo(
            ToolStripDropDownMenu menu,
            IBindingContext bindingContext)
        {
            BindTo(menu.Items, bindingContext);

            if (!(this.ContextSource is INotifyPropertyChanged))
            {
                //
                // The source isn't observable. Perform an explicit
                // refresh every time the menu is opened.
                //
                menu.Opening += (sender, args) => ForceRefresh();
            }
        }

        public void BindTo(
            ToolStripMenuItem menu,
            IBindingContext bindingContext)
        {
            BindTo(menu.DropDownItems, bindingContext);

            if (!(this.ContextSource is INotifyPropertyChanged))
            {
                //
                // The source isn't observable. Perform an explicit
                // refresh every time the menu is opened.
                //
                menu.DropDownOpening += (sender, args) => ForceRefresh();
            }
        }

        public ICommandContainer<TContext> AddCommand(IContextCommand<TContext> command)
            => AddCommand(command, null);

        public ICommandContainer<TContext> AddCommand(IContextCommand<TContext> command, int? index)
        {
            var item = new MenuItemViewModel(
                this.displayStyle,
                command,
                this);
            if (index != null)
            {
                this.menuItems.Insert(Math.Min(index.Value, this.menuItems.Count), item);
            }
            else
            {
                this.menuItems.Add(item);
            }

            //
            // Set initial state using current context.
            //
            item.OnContextUpdated();

            return new CommandContainer<TContext>(
                this.displayStyle,
                this.ContextSource,
                item.Children,
                this.bindingContext);
        }

        public void AddSeparator(int? index = null)
        {
            var item = new SeparatorViewModel();
            if (index != null)
            {
                this.menuItems.Insert(index.Value, item);
            }
            else
            {
                this.menuItems.Add(item);
            }
        }

        public void ExecuteCommandByKey(Keys keys)
        {
            this.menuItems
                .Where(i => i.ShortcutKeys == keys && i.IsVisible && i.IsEnabled)
                .FirstOrDefault()?
                .Invoke();
        }

        public void ExecuteDefaultCommand()
        {
            this.menuItems
                .Where(i => i.IsDefault && i.IsVisible && i.IsEnabled)
                .FirstOrDefault()?
                .Invoke();
        }

        internal abstract class MenuItemViewModelBase : ViewModelBase
        {
            private bool isVisible;
            private bool isEnabled;

            public MenuItemViewModelBase(
                ToolStripItemDisplayStyle displayStyle)
            {
                this.DisplayStyle = displayStyle;
                this.Children = new ObservableCollection<MenuItemViewModelBase>();
            }

            public ToolStripItemDisplayStyle DisplayStyle { get; }

            //-----------------------------------------------------------------
            // Virtual properties.
            //-----------------------------------------------------------------

            public virtual string? Text => null;

            public virtual string? ToolTip => null;

            public virtual Image? Image => null;

            public virtual Keys ShortcutKeys => Keys.None;

            public virtual bool IsSeparator => false;

            public virtual bool IsDefault => false;

            //-----------------------------------------------------------------
            // Mutable observable properties.
            //-----------------------------------------------------------------

            public ObservableCollection<MenuItemViewModelBase> Children { get; }

            public bool IsVisible
            {
                get => this.isVisible;
                set
                {
                    this.isVisible = value;
                    RaisePropertyChange();
                }
            }

            public bool IsEnabled
            {
                get => this.isEnabled;
                set
                {
                    this.isEnabled = value;
                    RaisePropertyChange();
                }
            }

            //-----------------------------------------------------------------
            // Actions.
            //-----------------------------------------------------------------

            public virtual void Invoke() { }
        }

        internal class SeparatorViewModel : MenuItemViewModelBase
        {
            public SeparatorViewModel()
                : base(ToolStripItemDisplayStyle.None)
            {
            }

            public override bool IsSeparator => true;
        }

        internal class MenuItemViewModel : MenuItemViewModelBase
        {
            private readonly IContextCommand<TContext> command;
            private readonly CommandContainer<TContext> container;

            public MenuItemViewModel(
                ToolStripItemDisplayStyle displayStyle,
                IContextCommand<TContext> command,
                CommandContainer<TContext> container)
                : base(displayStyle)
            {
                this.command = command;
                this.container = container;
            }

            internal void OnContextUpdated()
            {
                var context = this.container.ContextSource.Context;
                if (context == null)
                {
                    //
                    // Assume all commands are unavailable. Hiding
                    // all menu items looks awkward though, so disable
                    // them.
                    //
                    this.IsVisible = true;
                    this.IsEnabled = false;
                    return;
                }

                switch (this.command.QueryState(context))
                {
                    case CommandState.Disabled:
                        this.IsVisible = true;
                        this.IsEnabled = false;
                        break;

                    case CommandState.Enabled:
                        this.IsVisible = true;
                        this.IsEnabled = true;
                        break;

                    case CommandState.Unavailable:
                        this.IsVisible = false;
                        break;
                }

                OnContextUpdated(this.Children);
            }

            internal static void OnContextUpdated(
                IEnumerable<MenuItemViewModelBase> items)
            {
                foreach (var item in items.OfType<MenuItemViewModel>())
                {
                    item.OnContextUpdated();
                }
            }

            //-----------------------------------------------------------------
            // Read-only observable properties.
            //-----------------------------------------------------------------

            public override string? Text => this.command.Text;

            public override string? ToolTip
                => this.DisplayStyle == ToolStripItemDisplayStyle.Image
                    ? this.command.Text.Replace("&", "")
                    : null;

            public override Image? Image => this.command.Image;

            public override Keys ShortcutKeys => this.command.ShortcutKeys;

            public override bool IsDefault => this.command.IsDefault;

            //-----------------------------------------------------------------
            // Actions.
            //-----------------------------------------------------------------

            public override async void Invoke()
            {
                try
                {
                    var context = this.container.ContextSource.Context;
                    if (context == null)
                    {
                        //
                        // Context disappeared again, nevermind.
                        //
                        return;
                    }

                    await this.command
                        .ExecuteAsync(context)
                        .ConfigureAwait(true);

                    this.container.bindingContext.OnCommandExecuted(this.command);
                }
                catch (Exception e) when (e.IsCancellation())
                {
                    // Ignore.
                }
                catch (Exception e)
                {
                    this.container.bindingContext.OnCommandFailed(null, this.command, e);
                }
            }
        }
    }
}
