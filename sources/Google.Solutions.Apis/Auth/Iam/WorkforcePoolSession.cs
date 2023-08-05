using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Iam
{
    internal class WorkforcePoolSession : IOidcSession
    {
        private readonly UserCredential apiCredential;
        private readonly WorkforcePoolIdentity identity;

        public WorkforcePoolSession(
            UserCredential apiCredential, 
            WorkforcePoolIdentity identity)
        {
            this.apiCredential = apiCredential.ExpectNotNull(nameof(apiCredential));
            this.identity = identity.ExpectNotNull(nameof(identity));
        }


        //---------------------------------------------------------------------
        // IOidcSession.
        //---------------------------------------------------------------------

        public event EventHandler Terminated;

        public string Username => this.identity.Subject;

        public ICredential ApiCredential => this.apiCredential;

        public OidcOfflineCredential OfflineCredential
        {
            get
            {
                return new OidcOfflineCredential(
                    OidcIssuer.Iam,
                    this.apiCredential.Token.Scope,
                    this.apiCredential.Token.RefreshToken,
                    null);
            }
        }

        public void Splice(IOidcSession newSession)
        {
            throw new InvalidOperationException(
                "Workforce identity doesn't support reauthentication");
        }

        public Task RevokeGrantAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Workforce identity doesn't support revocation");
        }

        public void Terminate()
        {
            throw new InvalidOperationException(
                "Workforce identity doesn't support session termination");
        }
    }
}
