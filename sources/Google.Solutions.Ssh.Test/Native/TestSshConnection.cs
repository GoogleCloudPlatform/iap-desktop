using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{

    [TestFixture]
    public class TestSshConnection
    {
        private static SshSession CreateSession()
        {
            var session = new SshSession();
            session.SetTraceHandler(
                LIBSSH2_TRACE.SOCKET | LIBSSH2_TRACE.ERROR | LIBSSH2_TRACE.CONN |
                                       LIBSSH2_TRACE.AUTH | LIBSSH2_TRACE.KEX,
                Console.WriteLine);

            session.Timeout = TimeSpan.FromSeconds(5);
            return session;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public async Task WhenConnected_ThenGetRemoteBannerReturnsBanner(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.IsNotNull(connection.GetRemoteBanner());
            }
        }


        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenActiveAlgorithmsAreSet(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.KEX     ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.HOSTKEY ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.CRYPT_CS));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.CRYPT_SC));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.MAC_CS  ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.MAC_SC  ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.COMP_CS ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.COMP_SC ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.LANG_CS ));
                Assert.IsNotNull(connection.GetActiveAlgorithms(LIBSSH2_METHOD.LANG_SC ));
            }
        }

        //---------------------------------------------------------------------
        // Host Key.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyReturnsKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.IsNotNull(connection.GetRemoteHostKey());
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyTypeReturnsEcdsa256(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.AreEqual(
                    LIBSSH2_HOSTKEY_TYPE.ECDSA_256,
                    connection.GetRemoteHostKeyTyoe());
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyHashReturnsKeyHash(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.MD5));
                Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA1));
                Assert.IsNotNull(connection.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA256));
            }
        }

        //---------------------------------------------------------------------
        // User auth.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenIsAuthenticatedIsFalse(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                Assert.IsFalse(connection.IsAuthenticated);
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetAuthenticationMethodsReturnsPublicKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
                var methods = connection.GetAuthenticationMethods(string.Empty);
                Assert.IsNotNull(methods);
                Assert.AreEqual(1, methods.Length);
                Assert.AreEqual("publickey", methods.First());
            }
        }

        //[Test]
        //public async Task WhenPublicKeyInvalid_ThenAuthenticateThrowsPublicKeyUnverified(
        //    [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        //{
        //    var endpoint = new IPEndPoint(
        //        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
        //        22);
        //    using (var session = CreateSession())
        //    using (var connection = await session.ConnectAsync(endpoint))
        //    {
        //        SshAssert.ThrowsNativeExceptionWithError(
        //            LIBSSH2_ERROR.PUBLICKEY_UNVERIFIED,
        //            () => connection.Authenticate("bob", new byte[] { 1, 2, 3, 4, 5, 6 }).Wait());
        //    }
        //}

        [Test]
        public async Task WhenPublicKeyValidButUnrecognized_ThenAuthenticateThrowsAuthenticationFailed(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                SshAssert.ThrowsNativeExceptionWithError(
                    LIBSSH2_ERROR.AUTHENTICATION_FAILED,
                    () => connection.Authenticate("invaliduser", key).Wait());
            }
        }

        [Test]
        public async Task WhenPublicKeyValidAndKnownFromMetadata_ThenAuthenticateThrowsAuthenticationSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            using (var key = new RSACng())
            {
                await InstanceUtil.AddPublicKeyToMetadata(
                    await instanceLocatorTask,
                    "testuser",
                    key);
                await connection.Authenticate("testuser", key);
            }
        }
    }
}
