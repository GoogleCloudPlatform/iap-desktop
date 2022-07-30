using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Surface
{
    public interface ICommandContextSource<TContext> : INotifyPropertyChanged
    {
        TContext Context { get; }
    }

    public class ContextSource<TContext> : ViewModelBase, ICommandContextSource<TContext>
    {
        private TContext context;

        public TContext Context
        {
            get => this.context;
            set
            {
                this.context = value;
                RaisePropertyChange();
            }
        }
    }

    // TODO: Rename class
    public sealed class NewCommandContainer<TContext> : ICommandContainer<TContext>, IDisposable
        where TContext : class
    {
        private readonly IDisposable bindings;
        private readonly ToolStripItemDisplayStyle displayStyle;
        private readonly ObservableCollection<MenuItemViewModelBase> menuItems;
        
        internal ICommandContextSource<TContext> ContextSource { get; }

        internal ObservableCollection<MenuItemViewModelBase> MenuItems => this.menuItems;

        public event EventHandler<ExceptionEventArgs> CommandFailed;

        private NewCommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            ICommandContextSource<TContext> contextSource,
            ObservableCollection<MenuItemViewModelBase> items)
        {
            this.displayStyle = displayStyle;
            this.menuItems = items;
            this.ContextSource = contextSource;

            this.bindings = this.ContextSource.OnPropertyChange(
                s => s.Context,
                context => {
                    MenuItemViewModel.OnContextUpdated(this.menuItems);
                });
        }
        private void OnCommandFailed(Exception e)
        {
            this.CommandFailed?.Invoke(this, new ExceptionEventArgs(e));
        }

        public NewCommandContainer(
            ToolStripItemDisplayStyle displayStyle,
            ICommandContextSource<TContext> contextSource)
            : this(
                  displayStyle, 
                  contextSource,
                  new ObservableCollection<MenuItemViewModelBase>())
        {
        }

        public void BindTo(
            ToolStripItemCollection view,
            IContainer container = null)
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
                container);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.bindings.Dispose();
        }

        //---------------------------------------------------------------------
        // ICommandContainer.
        //---------------------------------------------------------------------

        public ICommandContainer<TContext> AddCommand(ICommand<TContext> command)
            => AddCommand(command, null);

        public ICommandContainer<TContext> AddCommand(ICommand<TContext> command, int? index)
        {
            var item = new MenuItemViewModel(
                this.displayStyle,
                command,
                this);
            if (index != null)
            {
                this.menuItems.Insert(index.Value, item);
            }
            else
            {
                this.menuItems.Add(item);
            }

            //
            // Set initial state using current context.
            //
            item.OnContextUpdated();

            return new NewCommandContainer<TContext>(
                this.displayStyle,
                this.ContextSource,
                item.Children);
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

            public virtual string Text => null;

            public virtual string ToolTip => null;

            public virtual Image Image => null;

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
            private readonly ICommand<TContext> command;
            private readonly NewCommandContainer<TContext> container;

            public MenuItemViewModel(
                ToolStripItemDisplayStyle displayStyle,
                ICommand<TContext> command,
                NewCommandContainer<TContext> container)
                : base(displayStyle)
            {
                this.command = command;
                this.container = container;
            }

            internal void OnContextUpdated()
            {
                switch (this.command.QueryState(this.container.ContextSource.Context))
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

            public override string Text => this.command.Text;

            public override string ToolTip
                => this.DisplayStyle == ToolStripItemDisplayStyle.Image
                    ? command.Text.Replace("&", "")
                    : null;

            public override Image Image => this.command.Image;

            public override Keys ShortcutKeys => this.command.ShortcutKeys;

            public override bool IsDefault => this.command.IsDefault;

            //-----------------------------------------------------------------
            // Actions.
            //-----------------------------------------------------------------

            public override void Invoke()
            {
                try
                {
                    this.command.Execute(this.container.ContextSource.Context);
                }
                catch (Exception e) when (e.IsCancellation())
                {
                    // Ignore.
                }
                catch (Exception e)
                {
                    this.container.OnCommandFailed(e);
                }
            }
        }
    }
}
