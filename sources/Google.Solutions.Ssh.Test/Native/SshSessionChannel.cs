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

                using (var authSession = await connection.Authenticate("testuser", key))
                using (var channel = await authSession.OpenSessionChannel())
                {
                    await channel.SetEnvironmentVariable("FOO", "bar");
                }
            }
        }

        //---------------------------------------------------------------------
        // Process/Shell.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenConnected_ThenStartShellSucceeds(
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
                    await channel.StartShell();
                    
                    var bytesWritten = await channel.Write(Encoding.ASCII.GetBytes("whoami;exit"));
                    Assert.AreEqual(11, bytesWritten);

                    var buffer = new byte[1024];

                    var bytesRead = await channel.Read(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    StringAssert.Contains(
                        "testuser", 
                        Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.AreEqual("", channel.ExitSignal);
                }
            }
        }

        [Test]
        public async Task WhenCommandIsValid_ThenExecuteSucceeds(
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
                    await channel.Execute("whoami");

                    var buffer = new byte[1024];
                    var bytesRead = await channel.Read(buffer);
                    Assert.AreNotEqual(0, bytesRead);

                    Assert.AreEqual("testuser\n", Encoding.ASCII.GetString(buffer, 0, (int)bytesRead));

                    Assert.AreEqual(0, channel.ExitCode);
                    Assert.IsNull(channel.ExitSignal);
                }
            }
        }

        [Test]
        public async Task WhenCommandInvalid_ThenExecSucceedsAndStderrContainsError(
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
                    await channel.Execute("invalidcommand");

                    var buffer = new byte[1024];
                    var bytesRead = await channel.ReadStdErr(buffer);
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
