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

    public class CommandSurface<TContext> : ICommandSurface<TContext>
        where TContext : class
    {
        private readonly CommandContainer<TContext> commands;


        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public event EventHandler<ExceptionEventArgs> CommandFailed;

        public CommandSurface(ToolStripItemDisplayStyle displayStyle)
        {
            this.commands = new CommandContainer<TContext>(
                displayStyle,
                e => this.CommandFailed?.Invoke(this, new ExceptionEventArgs(e)),
                null);
        }

        public void ApplyTo(ToolStrip toolBar)
        {
            toolBar.Items.AddRange(this.commands.MenuItems.ToArray());

            this.commands.MenuItemsChanged += (s, e) =>
            {
                toolBar.Items.Clear();
                toolBar.Items.AddRange(this.commands.MenuItems.ToArray());
            };
        }

        public TContext CurrentContext
        {
            get => this.commands.Context;
            set => this.commands.Context = value;
        }

        //---------------------------------------------------------------------
        // ICommandSurface.
        //---------------------------------------------------------------------

        public ICommandContainer<TContext> Commands => this.commands;

    }

    /// <summary>
    /// Command container that can be bound to multiple controls.
    /// </summary>
    public class CommandContainer<TContext> : ICommandContainer<TContext>
        where TContext : class
    {
        private TContext context;
        private readonly CommandContainer<TContext> parent;
        private readonly List<ToolStripItem> menuItems = new List<ToolStripItem>();
        
        private readonly ToolStripItemDisplayStyle displayStyle;
        private readonly Action<Exception> exceptionHandler;

        public EventHandler<EventArgs> MenuItemsChanged;

        protected void OnMenuItemsChanged()
        {
            if (this.parent != null)
            {
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
            IEnumerable<ToolStripItem> menuItems,
            TContext context)
        {
            // Update state of each menu item.
            foreach (var menuItem in menuItems
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

                //
                // NB. Only the top-most container has its context set.
                // Therefore, recursively update child menus as well.
                //

                if (menuItem is ToolStripDropDownItem dropDown)
                {
                    UpdateMenuItemState(
                        dropDown.DropDownItems.Cast<ToolStripDropDownItem>(),
                        context);
                }
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public CommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            Action<Exception> exceptionHandler,
            CommandContainer<TContext> parent)
        {
            this.displayStyle = displayStyle;
            this.exceptionHandler = exceptionHandler;
            this.parent = parent;
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

        public IList<ToolStripItem> MenuItems => this.menuItems;

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
                        Debug.Assert(this.Context != null);
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

            OnMenuItemsChanged(); // TODO: Test

            // Return a new contains that enables registering sub-commands.
            return new CommandContainer<TContext>(
                this.displayStyle,
                this.exceptionHandler,
                this);
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

            OnMenuItemsChanged(); // TODO: Test
        }


        public void ExecuteCommandByKey(Keys keys)
        {
            //
            // Only search top-level menu.
            //
            var menuItem = this.menuItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(m => m.ShortcutKeys == keys);
            if (menuItem?.Tag is Command<TContext> command)
            {
                Debug.Assert(this.Context != null);
                if (command.QueryState(this.Context) == CommandState.Enabled)
                {
                    command.Execute(this.Context);
                }
            }
        }

        public void ExecuteDefaultCommand()
        {
            Debug.Assert(this.Context != null);

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
