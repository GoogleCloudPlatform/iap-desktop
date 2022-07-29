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
    /// Command container that can be bound to multiple controls.
    /// </summary>
    public class CommandContainer<TContext> : ICommandContainer<TContext> // TODO: make internal class.
        where TContext : class
    {
        private TContext context;
        private readonly CommandContainer<TContext> parent;
        private readonly System.Collections.IList /* of ToolStripMenuItem */ menuItems;

        private readonly ToolStripItemDisplayStyle displayStyle;
        private readonly Action<Exception> exceptionHandler;

        public EventHandler<EventArgs> MenuItemsChanged;

        protected void OnMenuItemsChanged()
        {
            if (this.parent != null)
            {
                Debug.Assert(this.MenuItemsChanged == null);

                //
                // Let the parent fire the event.
                //
                this.parent.OnMenuItemsChanged();
            }
            else
            {
                this.MenuItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static void UpdateMenuItemState(
            System.Collections.IList menuItems,
            TContext context)
        {
            // Update state of each menu item.
            foreach (var menuItem in menuItems
                .Cast<ToolStripItem>()
                .Where(m => m.Tag is ICommand<TContext>))
            {
                var command = (ICommand<TContext>)menuItem.Tag;
                switch (command.QueryState(context))
                {
                    case CommandState.Disabled:
                        menuItem.Visible = true;
                        menuItem.Enabled = false;
                        break;

                    case CommandState.Enabled:
                        menuItem.Visible = true;
                        menuItem.Enabled = true;
                        break;

                    case CommandState.Unavailable:
                        menuItem.Visible = false;
                        break;
                }

                //
                // NB. Only the top-most container has its context set.
                // Therefore, recursively update child menus as well.
                //

                if (menuItem is ToolStripDropDownItem dropDown)
                {
                    UpdateMenuItemState(
                        dropDown.DropDownItems,
                        context);
                }
            }
        }

        private CommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            Action<Exception> exceptionHandler,
            CommandContainer<TContext> parent,
            System.Collections.IList menuItems)
        {
            this.displayStyle = displayStyle;
            this.exceptionHandler = exceptionHandler;
            this.parent = parent;
            this.menuItems = menuItems;
        }


        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public CommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            Action<Exception> exceptionHandler)
            : this(displayStyle, 
                   exceptionHandler,
                   null,
                   new System.Collections.ArrayList())
        {
        }


        /// <summary>
        /// Set the context that determines the state of
        /// menu items.
        /// </summary>
        public TContext Context
        {
            get => this.context ?? (this.parent?.Context);
            set
            {
                this.context = value;

                UpdateMenuItemState(this.menuItems, value);
            }
        }

        public IEnumerable<ToolStripItem> MenuItems => this.menuItems
            .Cast<ToolStripItem>();

        /// <summary>
        /// Refresh the state of menu items.
        /// </summary>
        public void ForceRefresh()
        {
            UpdateMenuItemState(this.menuItems, this.context);
        }

        //---------------------------------------------------------------------
        // ICommandContainer.
        //---------------------------------------------------------------------

        public ICommandContainer<TContext> AddCommand(ICommand<TContext> command)
            => AddCommand(command, null);

        public ICommandContainer<TContext> AddCommand(ICommand<TContext> command, int? index)
        {
            var menuItem = new ToolStripMenuItem(
                command.Text,
                command.Image,
                (sender, args) =>
                {
                    try
                    {
                        command.Execute(this.Context);
                    }
                    catch (Exception e) when (e.IsCancellation())
                    {
                        // Ignore.
                    }
                    catch (Exception e)
                    {
                        this.exceptionHandler(e);
                    }
                })
            {
                Tag = command,
                ShortcutKeys = command.ShortcutKeys,

                // If only an image is displayed (typically in a toolbar),
                // display the text as tool tip - but without the mnemonics.
                DisplayStyle = this.displayStyle,
                ToolTipText = this.displayStyle == ToolStripItemDisplayStyle.Image
                    ? command.Text.Replace("&", "")
                    : null
            };

            if (index.HasValue)
            {
                this.menuItems.Insert(Math.Min(index.Value, this.menuItems.Count), menuItem);
            }
            else
            {
                this.menuItems.Add(menuItem);
            }

            OnMenuItemsChanged();

            // Return a new contains that enables registering sub-commands.
            return new CommandContainer<TContext>(
                this.displayStyle,
                this.exceptionHandler,
                this,
                menuItem.DropDownItems);
        }

        public void AddSeparator(int? index = null)
        {
            var menuItem = new ToolStripSeparator();

            if (index.HasValue)
            {
                this.menuItems.Insert(Math.Min(index.Value, this.menuItems.Count), menuItem);
            }
            else
            {
                this.menuItems.Add(menuItem);
            }

            OnMenuItemsChanged();
        }


        public void ExecuteCommandByKey(Keys keys)
        {
            if (this.Context == null)
            {
                return;
            }

            //
            // Only search top-level menu.
            //
            var menuItem = this.menuItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(m => m.ShortcutKeys == keys);
            if (menuItem?.Tag is Command<TContext> command)
            {
                if (command.QueryState(this.Context) == CommandState.Enabled)
                {
                    command.Execute(this.Context);
                }
            }
        }

        public void ExecuteDefaultCommand()
        {
            if (this.Context == null)
            {
                return;
            }

            //
            // Only search top-level menu.
            //
            var firstDefaultCommand = this.menuItems
                .OfType<ToolStripMenuItem>()
                .Select(item => item.Tag)
                .EnsureNotNull()
                .OfType<Command<TContext>>()
                .Where(cmd => cmd.IsDefault)
                .Where(cmd => cmd.QueryState(this.Context) == CommandState.Enabled)
                .FirstOrDefault();
            firstDefaultCommand?.Execute(this.Context);
        }
    }
}
