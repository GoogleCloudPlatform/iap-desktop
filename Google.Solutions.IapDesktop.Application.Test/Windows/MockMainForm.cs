using Google.Apis.Auth.OAuth2;
using Google.Solutions.Compute.Auth;
using Google.Solutions.Compute.Test.Env;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    public partial class MockMainForm : Form, IMainForm, IJobHost, IAuthorizationService
    {
        public MockMainForm()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------------
        // IMainForm.
        //---------------------------------------------------------------------

        public DockPanel MainPanel => this.dockPanel;

        //---------------------------------------------------------------------
        // IJobHost.
        //---------------------------------------------------------------------

        public ISynchronizeInvoke Invoker => this;

        public bool IsWaitDialogShowing => true;


        public void CloseWaitDialog()
        {
        }

        public bool ConfirmReauthorization()
        {
            return true;
        }

        public void ShowWaitDialog(JobDescription jobDescription, CancellationTokenSource cts)
        {
        }

        //---------------------------------------------------------------------
        // IAuthorizationService.
        //---------------------------------------------------------------------

        private class SimpleAuthorization : IAuthorization
        {
            public ICredential Credential { get; }

            public SimpleAuthorization(ICredential credential)
            {
                this.Credential = credential;
            }

            public Task ReauthorizeAsync()
            {
                throw new NotImplementedException();
            }

            public Task RevokeAsync()
            {
                throw new NotImplementedException();
            }
        }

        public IAuthorization Authorization => new SimpleAuthorization(Defaults.GetCredential());
    }
}
