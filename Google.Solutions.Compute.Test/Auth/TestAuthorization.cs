using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Solutions.Compute.Auth;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Auth
{
    [TestFixture]
    public class TestAuthorization
    {
        [Test]
        public async Task WhenNoExistingAuthPresent_TryLoadExistingAuthorizationAsyncReturnsNull()
        {
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets()
                {
                    ClientId = "id",
                    ClientSecret = "secret"
                },
                Scopes = new[] { "one", "two" },
                DataStore = new NullDataStore()
            };

            var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                initializer,
                "<html/>",
                CancellationToken.None);

            Assert.IsNull(authz);
        }

        [Test]
        public async Task WhenExistingAuthLacksScopes_TryLoadExistingAuthorizationAsyncReturnsNullAndExistingAuthzIsDeleted()
        {
            var tokenResponse = new TokenResponse()
            {
                RefreshToken = "rt",
                Scope = "one two" // lacks email scope
            };

            var dataStore = new Mock<IDataStore>();
            dataStore.Setup(ds => ds.GetAsync<TokenResponse>(It.IsAny<string>()))
                .Returns(Task.FromResult(tokenResponse));

            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets()
                {
                    ClientId = "id",
                    ClientSecret = "secret"
                },
                Scopes = new[] { "one", "two" },
                DataStore = dataStore.Object
            };

            var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                initializer,
                "<html/>",
                CancellationToken.None);

            Assert.IsNull(authz);

            dataStore.Verify(ds => ds.DeleteAsync<TokenResponse>(It.IsAny<string>()), Times.Once);
        }
    }
}
