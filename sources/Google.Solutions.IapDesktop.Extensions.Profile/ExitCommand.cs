using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Profile
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x100)]
    [Service]
    public class ExitCommand : MenuCommandBase<UserProfile>
    {
        private readonly IMainWindow mainWindow;

        public ExitCommand(IMainWindow mainWindow)
            : base("E&xit")
        {
            this.ShortcutKeys = Keys.Alt | Keys.F4;
            this.mainWindow = mainWindow.ExpectNotNull(nameof(mainWindow));
        }


        protected override bool IsAvailable(UserProfile _)
        {
            return true;
        }

        protected override bool IsEnabled(UserProfile _)
        {
            return true;
        }

        public override void Execute(UserProfile _)
        {
            this.mainWindow.Close();
        }
    }
}
