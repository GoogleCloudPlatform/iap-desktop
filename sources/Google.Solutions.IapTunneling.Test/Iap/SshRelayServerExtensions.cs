using Google.Solutions.IapTunneling.Iap;
using Google.Solutions.IapTunneling.Test.Net;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Iap
{
    internal static class SshRelayServerExtensions
    {
        public static async Task SendConnectSuccessSidAsync(
            this ServerWebSocketConnection server, 
            string sid)
        {
            var buffer = new byte[64];
            var bytes = SshRelayFormat.ConnectSuccessSid.Encode(buffer, sid);

            await server
                .SendBinaryFrameAsync(buffer, 0, (int)bytes)
                .ConfigureAwait(false);
        }

        public static async Task SendDataAsync(
            this ServerWebSocketConnection server,
            byte[] data)
        {
            var buffer = new byte[data.Length + 6];
            var bytes = SshRelayFormat.Data.Encode(buffer, data, 0, (uint)data.Length);

            await server
                .SendBinaryFrameAsync(buffer, 0, (int)bytes)
                .ConfigureAwait(false);
        }
    }
}
