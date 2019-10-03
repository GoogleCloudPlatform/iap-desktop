using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Compute.Auth;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Auth
{
    [TestFixture]
    public class TestRegistryStore
    {
        private const string RegistryPath = "Software\\Google\\Google.Solutions.Compute.Test";
        
        private readonly TokenResponse exampleTokenResponse = new TokenResponse()
        {
            AccessToken = "1234567890",
            IdToken = "abcdefg",
            RefreshToken = "ABCDEFG",
            TokenType = "test",
            Scope = "http://example.com"
        };

        [TearDown]
        public void TearDown()
        {
            try
            {
                RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                    .DeleteSubKey(RegistryPath);
            }
            catch (Exception)
            { }
        }

        [Test]
        public async Task GetAndSetSucceeds()
        {
            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                await store.StoreAsync("one", this.exampleTokenResponse);
                var readResponse = await store.GetAsync<TokenResponse>("one");
                
                Assert.AreEqual(this.exampleTokenResponse.AccessToken, readResponse.AccessToken);
                Assert.AreEqual(this.exampleTokenResponse.IdToken, readResponse.IdToken);
                Assert.AreEqual(this.exampleTokenResponse.RefreshToken, readResponse.RefreshToken);
                Assert.AreEqual(this.exampleTokenResponse.TokenType, readResponse.TokenType);
                Assert.AreEqual(this.exampleTokenResponse.Scope, readResponse.Scope);
            }
        }

        [Test]
        public async Task GetNonexistingKeyReturnsNull()
        {
            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                Assert.IsNull(await store.GetAsync<TokenResponse>("nonexisting"));
            }
        }

        [Test]
        public async Task DeleteExistingKeySucceeds()
        {
            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                await store.StoreAsync("one", this.exampleTokenResponse);
                await store.DeleteAsync<TokenResponse>("one");
                Assert.IsNull(await store.GetAsync<TokenResponse>("one"));
            }
        }

        [Test]
        public async Task ClearSucceeds()
        {
            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                await store.StoreAsync("one", this.exampleTokenResponse);
                await store.ClearAsync();
                Assert.IsNull(await store.GetAsync<TokenResponse>("one"));
            }
        }

        [Test]
        public async Task DeleteNonexistingKeySucceeds()
        {
            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                await store.DeleteAsync<TokenResponse>("nonexisting");
            }
        }
    }
}
