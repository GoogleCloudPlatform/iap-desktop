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
            var initializer = new StsCodeFlowInitializer(
                this.endpoint,
                this.deviceEnrollment,
                this.provider,
                this.registration.ToClientSecrets(),
                this.userAgent)
            {
                Scopes = new[] { Scopes.Cloud }
            };

            return new StsCodeFlow(initializer);
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

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class StsAuthorizationCodeRequestUrl : AuthorizationCodeRequestUrl
        {
            public StsAuthorizationCodeRequestUrl(Uri authorizationServerUrl) 
                : base(authorizationServerUrl)
            {
            }

            [RequestParameter("provider_name", RequestParameterType.Query)]
            public string ProviderName { get; set; }
        }

        internal class StsCodeFlow : AuthorizationCodeFlow
        {
            private readonly StsCodeFlowInitializer initializer;

            public StsCodeFlow(StsCodeFlowInitializer initializer) : base(initializer)
            {
                this.initializer = initializer;
            }

            public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri)
            {
                return new StsAuthorizationCodeRequestUrl(
                    new Uri(this.initializer.AuthorizationServerUrl))
                {
                    ClientId = base.ClientSecrets.ClientId,
                    Scope = string.Join(" ", base.Scopes),
                    RedirectUri = redirectUri,
                    ProviderName = this.initializer.Provider.ToString()
                };
            }
        }

        internal class StsCodeFlowInitializer : AuthorizationCodeFlow.Initializer
        {
            private const string StsAuthorizationUrl = "https://auth.cloud.google/authorize";

            public WorkforcePoolProviderLocator Provider { get; set; }

            protected StsCodeFlowInitializer(
                ServiceEndpointDirections directions,
                IDeviceEnrollment deviceEnrollment,
                WorkforcePoolProviderLocator provider,
                ClientSecrets clientSecrets,
                UserAgent userAgent)
                : base(
                      StsAuthorizationUrl,
                      new Uri(directions.BaseUri, "/v1/oauthtoken").ToString())
            {
                this.Provider = provider;
                this.ClientSecrets = clientSecrets;

                //
                // Unlike the Gaia API, the /v1/oauthtoken ignores client secrets when
                // passed as POST parameters. Therefore, inject them as header too.
                //
                var clientSecretAuth = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{clientSecrets.ClientId}:{clientSecrets.ClientSecret}"));

                this.HttpClientFactory = new AuthenticatedClientFactory(
                    new PscAndMtlsAwareHttpClientFactory(
                        directions,
                        deviceEnrollment,
                        userAgent),
                    new AuthenticationHeaderValue("Basic", clientSecretAuth));

                ApiTraceSources.Default.TraceInformation(
                    "Using endpoint {0} and client {1}",
                    directions,
                    clientSecrets.ClientId);
            }

            public StsCodeFlowInitializer(
                ServiceEndpoint<WorkforcePoolClient> endpoint,
                IDeviceEnrollment deviceEnrollment,
                WorkforcePoolProviderLocator provider,
                ClientSecrets clientSecrets,
                UserAgent userAgent)
                : this(
                      endpoint.GetDirections(deviceEnrollment.State),
                      deviceEnrollment,
                      provider, 
                      clientSecrets,
                      userAgent)
            {
            }
        }

        private class AuthenticatedClientFactory : IHttpClientFactory
        {
            private readonly IHttpClientFactory factory;
            private readonly AuthenticationHeaderValue authenticationHeader;

            public AuthenticatedClientFactory(
                IHttpClientFactory factory, 
                AuthenticationHeaderValue authenticationHeader)
            {
                this.factory = factory;
                this.authenticationHeader = authenticationHeader;
            }

            public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
            {
                var client = this.factory.CreateHttpClient(args);
                client.DefaultRequestHeaders.Authorization = this.authenticationHeader;
                return client;
            }
        }
    }
}
