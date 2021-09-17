using Google.Solutions.IapTunneling.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Test.Net
{
    [TestFixture] 
    public class TestBufferedNetworkStream : IapFixtureBase
    {
        private class StaticStream : INetworkStream
        {
            public Queue<byte[]> ReadData { get; } = new Queue<byte[]>();
            public Queue<byte[]> WriteData { get; } = new Queue<byte[]>();

            public int MaxWriteSize => int.MaxValue;

            public int MinReadSize => 0;

            public Task CloseAsync(CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public void Dispose()
            { }

            public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (!this.ReadData.Any())
                {
                    return Task.FromResult(0);
                }

                var data = this.ReadData.Dequeue();

                var read = Math.Min(data.Length, count);
                Array.Copy(data, 0, buffer, offset, read);
                return Task.FromResult(read);
            }
        }

        [Test]
        public async Task WhenDataIsScattered_ThenReadFillsBuffer()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 1 });
            stream.ReadData.Enqueue(new byte[] { 2 });
            stream.ReadData.Enqueue(new byte[] { 3 });

            var bufferedStream = new BufferedNetworkStream(stream);

            var readBuffer = new byte[3];
            var bytesRead = await bufferedStream.ReadAsync(
                    readBuffer,
                    0,
                    readBuffer.Length,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(3, bytesRead);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, readBuffer);
        }

        [Test]
        public async Task WhenUnderlyingStreamEndsBeforeBufferIsFull_ThenReadFillsBuffer()
        {
            var stream = new StaticStream();
            stream.ReadData.Enqueue(new byte[] { 1 });
            stream.ReadData.Enqueue(new byte[] { 2 });

            var bufferedStream = new BufferedNetworkStream(stream);

            var readBuffer = new byte[3];
            var bytesRead = await bufferedStream.ReadAsync(
                    readBuffer,
                    0,
                    readBuffer.Length,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, bytesRead);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 0 }, readBuffer);
        }
    }
}
