using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
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
}
