using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Ssh;
using System.Net;

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    [Service]
    public class SshViewModel : ViewModelBase
    {
        //---------------------------------------------------------------------
        // Initialization properties.
        //---------------------------------------------------------------------

        public InstanceLocator? Instance { get; set; }
        public IPEndPoint? Endpoint { get; set; }
        public SshParameters? Parameters { get; set; }
        public ISshCredential? Credential { get; set; }


        protected override void OnValidate()
        {
            this.Instance.ExpectNotNull(nameof(this.Instance));
            this.Endpoint.ExpectNotNull(nameof(this.Endpoint));
            this.Parameters.ExpectNotNull(nameof(this.Parameters));
            this.Credential.ExpectNotNull(nameof(this.Credential));
        }
    }
}
