using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Http;
using Google.Apis.Util;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// Client for "headful" workforce identity "3PI" OAuth.
    /// </summary>
    public class WorkforcePoolClient : OidcClientBase
    {
        private readonly ServiceEndpoint<WorkforcePoolClient> endpoint;
        private readonly OidcClientRegistration registration;
        private readonly IDeviceEnrollment deviceEnrollment;
        private readonly WorkforcePoolProviderLocator provider;
        private readonly UserAgent userAgent;

        public WorkforcePoolClient( // TODO: Inject StsClioent
            ServiceEndpoint<WorkforcePoolClient> endpoint,
            IDeviceEnrollment deviceEnrollment,
            IOidcOfflineCredentialStore store,
            WorkforcePoolProviderLocator provider,
            OidcClientRegistration registration,
            UserAgent userAgent)
            : base(store)
        {
            this.endpoint = endpoint.ExpectNotNull(nameof(endpoint));
            this.registration = registration.ExpectNotNull(nameof(registration));
            this.deviceEnrollment = deviceEnrollment.ExpectNotNull(nameof(deviceEnrollment));
            this.provider = provider.ExpectNotNull(nameof(provider));
            this.userAgent = userAgent.ExpectNotNull(nameof(userAgent));

            Precondition.Expect(registration.Issuer == OidcIssuer.Iam, "Issuer");
        }

        public static ServiceEndpoint<WorkforcePoolClient> CreateEndpoint(
            PrivateServiceConnectDirections pscDirections = null)
        {
            return new ServiceEndpoint<WorkforcePoolClient>(
                pscDirections ?? PrivateServiceConnectDirections.None,
                "https://sts.googleapis.com/");
        }

        //---------------------------------------------------------------------
        // IClient.
        //---------------------------------------------------------------------

        public override IServiceEndpoint Endpoint => this.endpoint;

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected virtual IAuthorizationCodeFlow CreateFlow()
        {
            var initializer = new AuthPortalCodeFlow.Initializer(
                this.endpoint,
                this.deviceEnrollment,
                this.provider,
                this.registration.ToClientSecrets(),
                this.userAgent)
            {
                Scopes = new[] { Scopes.Cloud }
            };

            return new AuthPortalCodeFlow(initializer);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override async Task<IOidcSession> AuthorizeWithBrowserAsync(
            OidcOfflineCredential offlineCredential, 
            ICodeReceiver codeReceiver, 
            CancellationToken cancellationToken)
        {
            Precondition.Expect(offlineCredential == null ||
                offlineCredential.Issuer == OidcIssuer.Iam,
                "Offline credential must be issued by STS");

            codeReceiver.ExpectNotNull(nameof(codeReceiver));

            var flow = CreateFlow();
            var app = new AuthorizationCodeInstalledApp(flow, codeReceiver);

            var apiCredential = await
                app.AuthorizeAsync(null, cancellationToken)
                .ConfigureAwait(true);

            //
            // NB. The API does OAuth, not OIDC, so we don't receive an ID token.
            // To get information about the user, we have to introspect the
            // access token.
            //
            Debug.Assert(apiCredential.Token.IdToken == null);
            
            try
            {
                //TODO: introspect
                var identity = new WorkforcePoolIdentity("mock", "mock", "mock");

                return new WorkforcePoolSession(
                    apiCredential,
                    identity);
            }
            catch
            {
                flow.Dispose();
                throw;
            }
        }

        protected override Task<IOidcSession> ActivateOfflineCredentialAsync(
            OidcOfflineCredential offlineCredential,
            CancellationToken cancellationToken)
        {
            // TODO: ActivateOfflineCredentialAsync
            throw new NotImplementedException();
        }
    }
}
