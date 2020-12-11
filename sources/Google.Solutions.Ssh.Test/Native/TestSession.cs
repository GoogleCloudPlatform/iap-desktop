using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public async Task WhenPreferringIncompatibleAlgorithm_ThenHandshakeFails(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    session.SetPreferredMethods(LIBSSH2_METHOD.KEX, new[] { "diffie-hellman-group-exchange-sha1" });

                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                        22);

                    AssertEx.ThrowsAggregateException<SshNativeException>(
                        () => session.HandshakeAsync(socket).Wait());
                }
            }
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenGetRemoteBannerReturnsNull()
        {
            using (var session = CreateSession())
            {
                Assert.IsNull(session.GetRemoteBanner());
            }
        }

        public async Task WhenConnected_ThenGetRemoteBannerReturnsBanner(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                        22);
                    await session.HandshakeAsync(socket);

                    Assert.IsNotNull(session.GetRemoteBanner());
                }
            }
        }

        //---------------------------------------------------------------------
        // Host Key.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotConnected_ThenGetRemoteHostKeyReturnsNull()
        {
            using (var session = CreateSession())
            {
                Assert.IsNull(session.GetRemoteHostKey());
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyReturnsKey(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                        22);
                    await session.HandshakeAsync(socket);

                    Assert.IsNotNull(session.GetRemoteHostKey());
                }
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyTypeReturnsEcdsa256(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                        22);
                    await session.HandshakeAsync(socket);

                    Assert.AreEqual(
                        LIBSSH2_HOSTKEY_TYPE.ECDSA_256,
                        session.GetRemoteHostKeyTyoe());
                }
            }
        }

        [Test]
        public void WhenNotConnected_ThenGetRemoteHostKeyHashReturnsNull()
        {
            using (var session = CreateSession())
            {
                Assert.IsNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA1));
                Assert.IsNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA256));
                Assert.IsNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.MD5));
            }
        }

        [Test]
        public async Task WhenConnected_ThenGetRemoteHostKeyHashReturnsKeyHash(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask),
                        22);
                    await session.HandshakeAsync(socket);

                    Assert.IsNotNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.MD5));
                    Assert.IsNotNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA1));
                    Assert.IsNotNull(session.GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH.SHA256));
                }
            }
        }

        //---------------------------------------------------------------------
        // Handshake.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSocketNotConnected_ThenHandshakeThrowsException()
        {
            using (var session = CreateSession())
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await session.HandshakeAsync(socket);
                    Assert.Fail("Expected exception");
                }
                catch (SshNativeException e)
                {
                    Assert.AreEqual(LIBSSH2_ERROR.SOCKET_RECV, e.ErrorCode);
                }
            }
        }

        [Test]
        public async Task WhenSocketConnected_ThenHandshakeSucceeds(
            [LinuxInstance] ResourceTask<InstanceLocator> instanceLocatorTask)
        {
            using (var session = CreateSession())
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(
                        await InstanceUtil.PublicIpAddressForInstanceAsync(await instanceLocatorTask), 
                        22);
                    await session.HandshakeAsync(socket);
                }
            }
        }
    }
}
