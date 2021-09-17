using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Socks5;
using Google.Solutions.IapTunneling.Test.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Socks
{
    [TestFixture]
    public class TestSocks5Listener : IapFixtureBase
    {
        private static Socks5Stream ConnectToListener(Socks5Listener listener)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, listener.ListenPort));

            return new Socks5Stream(
                new SocketStream(socket, new ConnectionStatistics()));
        }

        [Test]
        public async Task WhenProtocolVersionInvalid_ThenServerSendsNoAcceptableMethods()
        {
            var relay = new Mock<ISocks5Relay>();
            var listener = new Socks5Listener(
                relay.Object,
                PortFinder.FindFreeLocalPort());

            var cts = new CancellationTokenSource();
            var listenTask = listener.ListenAsync(cts.Token);

            try
            {
                using (var clientStream = ConnectToListener(listener))
                {
                    await clientStream.WriteNegotiateMethodRequestAsync(
                            new NegotiateMethodRequest(
                                1,
                                new AuthenticationMethod[]
                                {
                                    AuthenticationMethod.NoAuthenticationRequired
                                }),
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    var response = await clientStream.ReadNegotiateMethodResponseAsync(
                            CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.AreEqual(Socks5Stream.ProtocolVersion, response.Version);
                    Assert.AreEqual(AuthenticationMethod.NoAcceptableMethods, response.Method);
                }
            }
            finally
            {
                cts.Cancel();
                await listenTask.ConfigureAwait(false);
            }
        }

        [Test]
        public void WhenAuthenticationMethodsUnsupported_ThenServerSendsNoAcceptableMethods()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void WhenConnectionCommandUnsupported_ThenServerSendsGeneralServerFailure()
        {
            Assert.Inconclusive();
        }
    }
}
