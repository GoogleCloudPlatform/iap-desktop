using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{
    [TestFixture]
    public class TestSession
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
        // Algorithms.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRequestingKex_ThenSupportedAlgorithmsIncludesDiffieHellman()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.KEX);

                CollectionAssert.Contains(algorithms, "diffie-hellman-group-exchange-sha256");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group-exchange-sha1");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group14-sha1");
                CollectionAssert.Contains(algorithms, "diffie-hellman-group1-sha1");
            }
        }

        [Test]
        public void WhenRequestingHostkey_ThenSupportedAlgorithmsIncludesRsaAndDss()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.HOSTKEY);

                CollectionAssert.Contains(algorithms, "ssh-rsa");
                CollectionAssert.Contains(algorithms, "ssh-dss");
            }
        }

        [Test]
        public void WhenRequestingCryptCs_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.CRYPT_CS);

                CollectionAssert.Contains(algorithms, "aes128-ctr");
                CollectionAssert.Contains(algorithms, "aes256-ctr");
            }
        }

        [Test]
        public void WhenRequestingCryptSc_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.CRYPT_SC);

                CollectionAssert.Contains(algorithms, "aes128-ctr");
                CollectionAssert.Contains(algorithms, "aes256-ctr");
            }
        }

        [Test]
        public void WhenRequestingMacSc_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.MAC_SC);

                CollectionAssert.Contains(algorithms, "hmac-sha2-256");
                CollectionAssert.Contains(algorithms, "hmac-sha2-512");
            }
        }

        [Test]
        public void WhenRequestingMacCs_ThenSupportedAlgorithmsIncludesAes()
        {
            using (var session = CreateSession())
            {
                var algorithms = session.GetSupportedAlgorithms(LIBSSH2_METHOD.MAC_CS);

                CollectionAssert.Contains(algorithms, "hmac-sha2-256");
                CollectionAssert.Contains(algorithms, "hmac-sha2-512");
            }
        }

        [Test]
        public void WhenRequestingInvalidType_ThenSupportedAlgorithmsThrowsException()
        {
            using (var session = CreateSession())
            {
                Assert.Throws<SshNativeException>(
                    () => session.GetSupportedAlgorithms((LIBSSH2_METHOD)Int32.MaxValue));
            }
        }

        [Test]
        public async Task WhenPreferringIncompatibleAlgorithm_ThenConnectFails(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            {
                session.SetPreferredMethods(LIBSSH2_METHOD.KEX, new[] { "diffie-hellman-group-exchange-sha1" });
                AssertEx.ThrowsAggregateException<SshNativeException>(
                    () => session.ConnectAsync(endpoint).Wait());
            }
        }

        //---------------------------------------------------------------------
        // Handshake/Connect.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenPortIsCorrect_ThenHandshakeSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                22);
            using (var session = CreateSession())
            using (var connection = await session.ConnectAsync(endpoint))
            {
            }
        }

        [Test]
        public async Task WhenPortNotListening_ThenHandshakeThrowsSocketException(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                12);
            using (var session = CreateSession())
            {
                AssertEx.ThrowsAggregateException<SocketException>(
                    () => session.ConnectAsync(endpoint).Wait());
            }
        }

        [Test]
        public async Task WhenPortIsNotSsh_ThenHandshakeTimesOut(
            [LinuxInstance(InitializeScript = InitializeScripts.InstallEchoServer)] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            var endpoint = new IPEndPoint(
                await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                7);
            using (var session = CreateSession())
            {
                try
                {
                    await session.ConnectAsync(endpoint);
                    Assert.Fail("Expected exception");
                }
                catch (Exception e)
                {
                    Assert.IsInstanceOf(typeof(SshNativeException), e);
                    Assert.AreEqual(LIBSSH2_ERROR.TIMEOUT, ((SshNativeException)e).ErrorCode);
                }
            }
        }
    }
}
