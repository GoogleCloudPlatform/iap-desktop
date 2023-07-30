using Google.Solutions.Apis.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth
{
    public class ByoidOidcClient : OidcClientBase
    {
        public ByoidOidcClient(
            IDeviceEnrollment deviceEnrollment, 
            IOidcOfflineCredentialStore store) 
            : base(deviceEnrollment, store)
        {
        }

        // TODO: WorkforceIdentityClient, cf https://docs.google.com/document/d/1wVqW62U-BMXnSlNdS962ixPPeUf-IN-MJjuZduEfgIU/edit?resourcekey=0-Oc-wjfuG9RWL5-yjnkiltg#heading=h.l5nfl0rnl0va
        public override IServiceEndpoint Endpoint => throw new NotImplementedException();

        protected override Task<OidcAuthorization> ActivateOfflineCredentialAsync(
            OAuthOfflineCredential offlineCredential, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<OidcAuthorization> AuthorizeWithBrowserAsync(
            OAuthOfflineCredential offlineCredential, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
