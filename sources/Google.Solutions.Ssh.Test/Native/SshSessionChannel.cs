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
    public class SshSessionChannel : SshFixtureBase
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
                using (var channel = await authSession.OpenShellChannelAsync())
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
                using (var channel = await authSession.OpenShellChannelAsync())
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

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenCommandIsValid_ThenOpenExecChannelAsyncSucceeds(
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
                using (var channel = await authSession.OpenExecChannelAsync("whoami"))
                {
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual("testuser\n", Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                }
            }
        }

        [Test]
        public async Task WhenCommandInvalid_ThenExecuteSucceedsAndStderrContainsError(
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
                using (var channel = await authSession.OpenExecChannelAsync("invalidcommand"))
                {
                    await channel.CloseAsync();

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadAsync(
                        LIBSSH2_STREAM.EXTENDED_DATA_STDERR, 
                        buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual(
                        "bash: invalidcommand: command not found\n", 
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(127, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                }
            }
        }
    }
}
