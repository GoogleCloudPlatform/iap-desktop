using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Net
{
    public sealed class BufferedNetworkStream : INetworkStream
    {
        private readonly INetworkStream stream;

        public BufferedNetworkStream(INetworkStream stream)
        {
            this.stream = stream;
        }

        public int MaxWriteSize => this.stream.MaxWriteSize;

        public int MinReadSize => this.stream.MinReadSize;

        public Task CloseAsync(CancellationToken cancellationToken)
            => this.stream.CloseAsync(cancellationToken);

        public void Dispose()
            => this.stream.Dispose();

        public Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => this.stream.WriteAsync(buffer, offset, count, cancellationToken);

        public async Task<int> ReadAsync(
            byte[] buffer, 
            int offset, 
            int count, 
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;

            //
            // Keep reading until we have the requested amount of data.
            //
            while (totalBytesRead < count)
            {
                var bytesRead = await this.stream.ReadAsync(
                        buffer,
                        offset + totalBytesRead,
                        count - totalBytesRead,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }
                else
                {
                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }
    }
}
