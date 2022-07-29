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
    public interface ICommandSurface<TContext>
        where TContext : class
    {
        ICommandContainer<TContext> Commands { get; }
    }

    public class ToolStripCommandSurface<TContext> : ICommandSurface<TContext>
        where TContext : class
    {
        private readonly CommandContainer<TContext> commands;


        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public event EventHandler<ExceptionEventArgs> CommandFailed;

        public ToolStripCommandSurface(ToolStripItemDisplayStyle displayStyle)
        {
            this.commands = new CommandContainer<TContext>(
                displayStyle,
                e => this.CommandFailed?.Invoke(this, new ExceptionEventArgs(e)),
                null);
        }

        public void ApplyTo(ToolStrip menu)
        {
            //
            // Populate eagerly.
            //
            menu.Items.AddRange(this.commands.MenuItems.ToArray());

            this.commands.MenuItemsChanged += (s, e) =>
            {
                var oldMenuItemsWithCommand = this.commands
                    .MenuItems
                    .Where(i => i.Tag is ICommand<TContext>)
                    .ToList();

                foreach (var item in oldMenuItemsWithCommand)
                {
                    menu.Items.Remove(item);
                }

                menu.Items.AddRange(this.commands.MenuItems.ToArray());
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

    public class MenuCommandSurface<TContext> : ICommandSurface<TContext>
        where TContext : class
    {
        private readonly ICommandSurfaceContextSource<TContext> source;
        private readonly CommandContainer<TContext> commands;

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public event EventHandler<ExceptionEventArgs> CommandFailed;

        public MenuCommandSurface(
            ToolStripItemDisplayStyle displayStyle,
            ICommandSurfaceContextSource<TContext> source)
        {
            this.source = source;
            this.commands = new CommandContainer<TContext>(
                displayStyle,
                e => this.CommandFailed?.Invoke(this, new ExceptionEventArgs(e)),
                null);
        }

        public void ApplyTo(ToolStripDropDown dropDownMenu)
        {
            //
            // Populate lazily when opened.
            //
            dropDownMenu.Opening += (s, a) =>
            {
                //
                // Query and set new context.
                //
                this.commands.Context = this.source.CurrentContext;

                //
                // Update menu items.
                //
                dropDownMenu.Items.Clear();
                dropDownMenu.Items.AddRange(this.commands.MenuItems.ToArray());
            };
        }

        //---------------------------------------------------------------------
        // ICommandSurface.
        //---------------------------------------------------------------------

        public ICommandContainer<TContext> Commands => this.commands;
    }

    public interface ICommandSurfaceContextSource<TContext>
    {
        TContext CurrentContext { get; }
    }
}
