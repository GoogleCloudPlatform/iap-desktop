using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.ActiveDirectory
{
    public class JoinViewModel : ViewModelBase
    {
        public JoinViewModel()
        {
            this.DomainName = ObservableProperty.Build(string.Empty);
            this.IsDomainNameInvalid = ObservableProperty.Build(
                this.DomainName,
                name => !string.IsNullOrWhiteSpace(name) && !name.Contains('.'));

            this.ComputerName = ObservableProperty.Build(string.Empty);
            this.IsComputerNameInvalid = ObservableProperty.Build(
                this.ComputerName,
                name => !string.IsNullOrWhiteSpace(name) && name.Length > 15);

            this.IsOkButtonEnabled = ObservableProperty.Build(
                this.DomainName,
                this.ComputerName,
                (string domain, string computer) =>
                    !string.IsNullOrEmpty(domain) && domain.Contains('.') &&
                    !string.IsNullOrEmpty(computer) && computer.Trim().Length <= 15);
        }

        //---------------------------------------------------------------------
        // Observable "output" properties.
        //---------------------------------------------------------------------

        public ObservableProperty<string> DomainName { get; }

        public ObservableFunc<bool> IsDomainNameInvalid { get; }

        public ObservableProperty<string> ComputerName { get; }

        public ObservableFunc<bool> IsComputerNameInvalid { get; }

        public ObservableFunc<bool> IsOkButtonEnabled { get; }
    }
}
