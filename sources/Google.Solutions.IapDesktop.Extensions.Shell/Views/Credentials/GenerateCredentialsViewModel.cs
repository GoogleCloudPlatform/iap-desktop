using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.Credentials
{
    public class GenerateCredentialsViewModel : ViewModelBase
    {
        // SAM usernames do not permit these characters, see
        // https://docs.microsoft.com/en-us/windows/desktop/adschema/a-samaccountname
        private readonly string DisallowsCharactersInUsername = "\"/\\[]:;|=,+*?<>";

        private bool addToAdministrators = true;
        private string username = string.Empty;

        public bool IsAllowedCharacterForUsername(char c)
            => !DisallowsCharactersInUsername.Contains(c);

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public bool AddToAdministrators
        {
            get => this.addToAdministrators;
            set
            {
                this.addToAdministrators = value;
                RaisePropertyChange();
            }
        }

        public string Username
        {
            get => this.username;
            set
            {
                this.username = value;
                RaisePropertyChange();
                RaisePropertyChange((GenerateCredentialsViewModel m) => m.IsOkButtonEnabled);
            }
        }

        public bool IsOkButtonEnabled
            => !string.IsNullOrWhiteSpace(this.username);
    }
}
