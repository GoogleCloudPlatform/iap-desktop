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
        public async Task GetUndecypherableValueReturnsNull()
        {
            // Store a value as if it had been encrypted with a different user's
            // DPAPI key.
            var valueFromOtherUser =
                "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAAmr8hZDsnM0KybdI4HPGUBgAAAAACAAAAAAAQZgAAAAEAACA" +
                "AAACl7vLBymJHa5kqvN5PBSTf2TXOvEL6rqYwF5AlRrgU8gAAAAAOgAAAAAIAACAAAACHJPnnyoGjDQ" +
                "UkKzbfmj5N7QQOE7izdhTIkuC6JDAYHNABAAAUdpXRCzMR2JVCQWe4FfQ/eeXYPhAndmTYSXHGFgr3N" +
                "r8ezULrsoc7Gic09DVEDV+t8MaTylYEizioq02gw0tX+23cPr8XlVlevxsfZw/CP35Ghi4C9maFOic8" +
                "AB7jZbaRXIbH4FWWksHmkBwNQ8uBWkgkmU8+X6NvBmJfIwcJ8nfPeJKQTzYbc8frapzqYfI3ynaa0t4" +
                "9YfMd1cLKGHM5Tldv0W+BS/lu3/vG3l1PXfzIYEuCg26jvCOmsBHS3f6ga3WCk8wd26f6GYKcbcXdju" +
                "euigofnzC56uoayxp5Ii5sBNS8JBXmi5g9AJUB3gK/falkf52xgizONB1dQFMVFHIaj6AKFxZwDD8wH" +
                "chzUthGPJDqvHlPWOGfPILc5poSfUBsh9oRcMTxlMhh+r8tpV+ZsOsbc3uIaLw8pXTwE5Z3UcoXxek0" +
                "99AA7I8CxBVYsRePwE9KSKX20mjATBUmCd0HOwmjh+F1zJK3DKMpjf1iGRHSjXxhay+Hxe8OvuJljxE" +
                "PzxQnCqTASbyUsnQQVDXtGLk6VJ0t/YT+B6wEOj3st+ZYmzBrOsPnsJLMSoOTQZ6+Lujpn4TGUeBtU1" +
                "GgqeTKoJG12J2SJyCvC9JK1UAAAABCgq/Mh/8A0GwpSpd44z/ydevpy8CUqmhZjsMNjT9eSRKCgErFh" +
                "PQmR4JrkL/eZfSr39sq72OrwLKZaOOM893a";

            using (var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                        .CreateSubKey(RegistryPath, true))
            {
                key.SetValue("otheruser", Convert.FromBase64String(valueFromOtherUser));
                //var val=  (byte[])key.GetValue("oauth");
                //var b64 = Convert.ToBase64String(val);
                //b64.ToString();
            }

            using (var store = new RegistryStore(RegistryHive.CurrentUser, RegistryPath))
            {
                Assert.IsNull(await store.GetAsync<TokenResponse>("otheruser"));
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
