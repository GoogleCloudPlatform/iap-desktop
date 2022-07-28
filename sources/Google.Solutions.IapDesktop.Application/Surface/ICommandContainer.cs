using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Surface
{
    public interface ICommandContainer<TContext>
        where TContext : class
    {
        ICommandContainer<TContext> AddCommand(
            ICommand<TContext> command);

        ICommandContainer<TContext> AddCommand(
            ICommand<TContext> command,
            int? index);

        void AddSeparator(int? index = null);
    }

    public interface ICommand<TContext>
    {
        string Text { get; }
        System.Drawing.Image Image { get; }
        Keys ShortcutKeys { get; }

        CommandState QueryState(TContext context);
        void Execute(TContext context);
    }

    public enum CommandState
    {
        Enabled,
        Disabled,
        Unavailable
    }

}
