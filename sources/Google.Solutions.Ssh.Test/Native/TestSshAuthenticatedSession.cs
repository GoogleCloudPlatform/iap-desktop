using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
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
    public class TestSshAuthenticatedSession : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDisconnected_ThenOpenChannelThrowsSocketSend(
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
                using (var authSession = await connection.Authenticate("testuser", key))
                {
                    connection.Dispose();
                    SshAssert.ThrowsNativeExceptionWithError(
                        session,
                        LIBSSH2_ERROR.SOCKET_SEND,
                        () => authSession.OpenSessionChannel().Wait());
                }
            }
        }

        [Test]
        public async Task WhenConnected_ThenOpenSessionChannelSucceeds  (
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
                using (var authSession = await connection.Authenticate("testuser", key))
                using (var channel = await authSession.OpenSessionChannel())
                {

                }
            }
        }
    }
}
