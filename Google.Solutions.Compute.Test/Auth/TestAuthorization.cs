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
            var adapter = new Mock<IAuthAdapter>();
            adapter.Setup(a => a.GetStoredRefreshTokenAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<TokenResponse>(null));
            
            var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                adapter.Object,
                CancellationToken.None);

            Assert.IsNull(authz);
        }

        [Test]
        public async Task WhenExistingAuthLacksScopes_TryLoadExistingAuthorizationAsyncReturnsNullAndExistingAuthzIsDeleted()
        {
            var tokenResponse = new TokenResponse()
            {
                RefreshToken = "rt",
                Scope = "one two"
            };

            var adapter = new Mock<IAuthAdapter>();
            adapter.Setup(a => a.GetStoredRefreshTokenAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(tokenResponse));
            adapter.Setup(a => a.IsRefreshTokenValid(tokenResponse))
                .Returns(true);
            adapter.SetupGet(a => a.Scopes)
                .Returns(new[] { "one", "two", "email" });

            var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                adapter.Object,
                CancellationToken.None);

            Assert.IsNull(authz);

            adapter.Verify(a => a.DeleteStoredRefreshToken(), Times.Once);
        }

        [Test]
        public async Task WhenExistingAuthIsOk_TryLoadExistingAuthorizationAsyncReturnsAuthorization()
        {
            var tokenResponse = new TokenResponse()
            {
                RefreshToken = "rt",
                Scope = "email one two"
            };

            var adapter = new Mock<IAuthAdapter>();
            adapter.Setup(a => a.GetStoredRefreshTokenAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(tokenResponse));
            adapter.Setup(a => a.IsRefreshTokenValid(tokenResponse))
                .Returns(true);
            adapter.SetupGet(a => a.Scopes)
                .Returns(new[] { "one", "two", "email" });

            var authz = await OAuthAuthorization.TryLoadExistingAuthorizationAsync(
                adapter.Object,
                CancellationToken.None);

            Assert.IsNotNull(authz);

            adapter.Verify(a => a.AuthorizeUsingRefreshToken(tokenResponse), Times.Once);
            adapter.Verify(a => a.QueryUserInfoAsync(
                It.IsAny<ICredential>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
