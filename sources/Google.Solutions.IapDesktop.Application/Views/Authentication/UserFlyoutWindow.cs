using Google.Solutions.Common.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Views.Authentication
{
    public partial class UserFlyoutWindow : FlyoutWindow
    {
        private readonly UserFlyoutViewModel viewModel;

        public UserFlyoutWindow(UserFlyoutViewModel viewModel)
        {
            this.viewModel = viewModel; 
            
            InitializeComponent();
        }
    }
}
