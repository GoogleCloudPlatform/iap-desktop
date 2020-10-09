using Google.Solutions.Common.Auth;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    public class UserFlyoutViewModel : ViewModelBase
    {
        public string Email { get; set; }

        public UserFlyoutViewModel(IAuthorization authorization)
        {
            this.Email = authorization.Email;
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void OpenMyAccountPage()
        {
            using (Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Verb = "open",
                FileName = "https://myaccount.google.com/security"
            }))
            { };
        }
    }
}
