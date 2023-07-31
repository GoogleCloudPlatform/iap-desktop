using Google.Apis.Auth.OAuth2;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Properties;
using Google.Solutions.Platform.Net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Profile.Auth
{
    internal class NewAuthorization : IAuthorization
    {
        private IOidcClient client;
        private IOidcSession session = null;

        private void SetOrSpliceSession(IOidcSession newSession)
        {
            newSession.ExpectNotNull(nameof(newSession));   

            if (this.session != null)
            {
                //
                // Once we have a session, we must never replace it,
                // all we can do it is splice it to extend its lifetime.
                //
                this.session.Splice(newSession);
            }
            else
            {
                this.session = newSession;
            }
        }

        public NewAuthorization(IOidcClient client)
        {
            this.client = client.ExpectNotNull(nameof(client));
        }

        //---------------------------------------------------------------------
        // IAuthorization.
        //---------------------------------------------------------------------

        public event EventHandler Reauthorized;

        public IOidcSession Session
        {
            get => this.session ?? throw new InvalidOperationException("Not authorized yet");
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------


        public Task RevokeAsync(CancellationToken cancellationToken)
        {
            return this.Session.RevokeGrantAsync(cancellationToken); // TODO: Test if null
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        /// <summary>
        /// Try to authorize using an existing refresh token.
        /// </summary>
        public async Task<bool> TryAuthorizeSilentlyAsync(
            CancellationToken cancellationToken)
        {
            Debug.Assert(
                this.session == null,
                "Silent authorize should only be performed initially");

            var newSession = await this.client
                .TryAuthorizeSilentlyAsync(cancellationToken) 
                .ConfigureAwait(false);

            if (newSession != null)
            {
                SetOrSpliceSession(newSession);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Authorize or re-authorize using a browser-based OIDC flow.
        /// </summary>
        public async Task AuthorizeAsync(
            BrowserPreference browserPreference,
            CancellationToken cancellationToken)
        {
            var newSession = await this.client
                .AuthorizeAsync(
                new BrowserCodeReceiver(browserPreference),
                cancellationToken)
                .ConfigureAwait(false);
            SetOrSpliceSession(newSession);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class BrowserCodeReceiver : LocalServerCodeReceiver 
        {
            private readonly BrowserPreference browserPreference;

            public BrowserCodeReceiver(BrowserPreference browserPreference)
                : base(Resources.AuthorizationSuccessful)
            {
                this.browserPreference = browserPreference;
            }

            protected override bool OpenBrowser(string url)
            {
                Browser
                    .Get(this.browserPreference)
                    .Navigate(url);
                return true;
            }
        }

        //---------------------------------------------------------------------
        // TODO: Obsolete methods below?

        public ICredential Credential => this.Session.ApiCredential;

        public string Email => this.Session.Username;

        public UserInfo UserInfo => new UserInfo()
        {
            Email = this.Session.Username,
            HostedDomain = (this.Session as IGaiaOidcSession)?.HostedDomain
        };

        public IDeviceEnrollment DeviceEnrollment => this.Session.DeviceEnrollment;

        public Task RevokeAsync()
        {
            throw new NotImplementedException();
        }

        public Task ReauthorizeAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
