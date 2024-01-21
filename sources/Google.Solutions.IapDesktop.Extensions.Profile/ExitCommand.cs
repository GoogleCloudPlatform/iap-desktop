using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Profile
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x100)]
    [Service]
    public class ExitCommand : MenuCommandBase<ProfileMenu.Context>
    {
        private readonly IMainWindow mainWindow;

        public ExitCommand(IMainWindow mainWindow)
            : base("E&xit")
        {
            this.ShortcutKeys = Keys.Alt | Keys.F4;
            this.mainWindow = mainWindow.ExpectNotNull(nameof(mainWindow));
        }


        protected override bool IsAvailable(ProfileMenu.Context context)
        {
            return true;
        }

        protected override bool IsEnabled(ProfileMenu.Context context)
        {
            return true;
        }

        public override void Execute(ProfileMenu.Context context)
        {
            this.mainWindow.Close();
        }
    }
}
