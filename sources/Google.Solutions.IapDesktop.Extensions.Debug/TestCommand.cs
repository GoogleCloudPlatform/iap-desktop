using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views;

namespace Google.Solutions.IapDesktop.Extensions.Debug
{
    [MenuCommand(typeof(DebugMenu))]
    [Service]
    public class TestCommand : MenuCommandBase<DebugMenu.Context>, IMenuCommand<DebugMenu>
    {
        public TestCommand() : base("&Test")
        {
        }

        protected override bool IsAvailable(DebugMenu.Context context)
        {
            return true;
        }

        protected override bool IsEnabled(DebugMenu.Context context)
        {
            return true;
        }
    }
}
