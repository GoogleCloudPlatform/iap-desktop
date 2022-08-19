using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.ActiveDirectory
{
    public partial class JoinDialog : Form
    {
        private readonly JoinViewModel viewModel;

        public JoinDialog()
        {
            InitializeComponent();

            this.headlineLabel.ForeColor = ThemeColors.HighlightBlue;

            this.viewModel = new JoinViewModel();
            this.domainText.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.DomainName,
                this.Container);
            this.domainWarning.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsDomainNameInvalid,
                this.Container);

            this.computerNameText.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.ComputerName,
                this.Container);
            this.computerNameWarning.BindReadonlyProperty(
                c => c.Visible,
                this.viewModel,
                m => m.IsComputerNameInvalid,
                this.Container);

            this.okButton.BindReadonlyProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsOkButtonEnabled,
                this.Container);
        }

        public ObservableProperty<string> ComputerName => this.viewModel.ComputerName;

        public ObservableProperty<string> DomainName => this.viewModel.DomainName;
    }
}
