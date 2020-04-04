using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Auth
{
    public interface IOAuthAdapter : IDisposable
    {
        Task<TokenResponse> GetStoredRefreshTokenAsync(CancellationToken token);
        
        bool IsRefreshTokenValid(TokenResponse tokenResponse);

        Task DeleteStoredRefreshToken();
        
        ICredential AuthorizeUsingRefreshToken(TokenResponse tokenResponse);
        
        Task<ICredential> AuthorizeUsingBrowserAsync(CancellationToken token);
    }

    public class GoogleOAuthAdapter : IOAuthAdapter
    {
        private readonly GoogleAuthorizationCodeFlow.Initializer initializer;
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly AuthorizationCodeInstalledApp installedApp;

        public GoogleOAuthAdapter(
            GoogleAuthorizationCodeFlow.Initializer initializer,
            string closePageReponse)
        {
            this.initializer = initializer;
            this.flow = new GoogleAuthorizationCodeFlow(initializer);
            this.installedApp = new AuthorizationCodeInstalledApp(
                this.flow,
                new LocalServerCodeReceiver(closePageReponse));
        }

        public Task<TokenResponse> GetStoredRefreshTokenAsync(CancellationToken token)
        {
            return this.flow.LoadTokenAsync(
                OAuthAuthorization.StoreUserId,
                token);
        }

        public Task DeleteStoredRefreshToken()
        {
            return this.initializer.DataStore.DeleteAsync<TokenResponse>(OAuthAuthorization.StoreUserId);
        }

        public ICredential AuthorizeUsingRefreshToken(TokenResponse tokenResponse)
        {
            return new UserCredential(
                new GoogleAuthorizationCodeFlow(this.initializer),
                    OAuthAuthorization.StoreUserId,
                    tokenResponse);
        }

        public async Task<ICredential> AuthorizeUsingBrowserAsync(CancellationToken token)
        {
            return await this.installedApp.AuthorizeAsync(
                OAuthAuthorization.StoreUserId,
                token);
        }

        public bool IsRefreshTokenValid(TokenResponse tokenResponse)
        {
            return !this.installedApp.ShouldRequestAuthorizationCode(tokenResponse);
        }

        public void Dispose()
        {
            this.flow.Dispose();
        }
    }
}
