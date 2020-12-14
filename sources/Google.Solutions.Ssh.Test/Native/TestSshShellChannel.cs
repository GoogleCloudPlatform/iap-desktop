using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Native
{

    [TestFixture]
    public class TestSshShellChannel : SshFixtureBase
    {
        //---------------------------------------------------------------------
        // Environment.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenSetEnvironmentVariableSucceeds(
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

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenShellChannelAsync(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE))
                {
                    await channel.SetEnvironmentVariableAsync("FOO", "bar");
                    await channel.CloseAsync();
                }
            }
        }

        //---------------------------------------------------------------------
        // Shell.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenOpenShellChannelAsyncSucceeds(
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

                using (var authSession = await connection.AuthenticateAsync("testuser", key))
                using (var channel = await authSession.OpenShellChannelAsync(
                    LIBSSH2_CHANNEL_EXTENDED_DATA.MERGE))
                {
                    var bytesWritten = await channel.WriteAsync(Encoding.ASCII.GetBytes("whoami;exit"));
                    Assert.AreEqual(11, bytesWritten);

                    //await channel.FlushAsync();
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    StringAssert.Contains(
                        "testuser", 
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.AreEqual("", channel.ExitSignal);
                }
            }
        }
    }
}
