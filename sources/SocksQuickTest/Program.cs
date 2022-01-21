using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Net;
using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Net;
using Google.Solutions.IapTunneling.Socks5;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocksQuickTest
{
    class Program : ISshRelayEndpointResolver
    {
        private const ushort SocksPort = 1080;
        private readonly UserAgent agent = new UserAgent("SocksTest", new Version(1, 0));
        private readonly ICredential credential;

        public Program(ICredential credential)
        {
            this.credential = credential;
        }

        public Task<ISshRelayEndpoint> ResolveEndpointAsync(
            string destinationDomain,
            ushort destinationPort,
            CancellationToken cancellationToken)
        {
            if (InternalDns.TryParseZonalDns(destinationDomain, out var locator))
            {
                return Task.FromResult<ISshRelayEndpoint>(new IapTunnelingEndpoint(
                    this.credential,
                    locator,
                    destinationPort,
                    IapTunnelingEndpoint.DefaultNetworkInterface,
                    this.agent));
            }
            else
            {
                throw new ArgumentException(
                    $"{destinationPort} is not a valid internal DNS name");
            }
        }

        public Task<ISshRelayEndpoint> ResolveEndpointAsync(
            IPAddress destination,
            ushort destinationPort,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task StartSocksServerAsync(CancellationToken cancellationToken)
        {
            var listener = new Socks5Listener(
                this,
                new AllowAllRelayPolicy(),
                SocksPort);
            await listener
                .ListenAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        static void Main(string[] args)
        {
            var chrome = Process.Start(new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = "chrome",
                Arguments = $"--user-data-dir={Path.GetTempPath()} " +
                            $"--proxy-server=\"socks5://127.0.0.1:{SocksPort}\" " +
                            "--guest --host-resolver-rules=\"MAP * ~NOTFOUND\""
            });

            new Program(GoogleCredential.GetApplicationDefault())
                .StartSocksServerAsync(CancellationToken.None)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Console.WriteLine(t.Exception.ToString());
                        Environment.Exit(1);
                    }
                });

            chrome.WaitForExit();
        }
    }
}
