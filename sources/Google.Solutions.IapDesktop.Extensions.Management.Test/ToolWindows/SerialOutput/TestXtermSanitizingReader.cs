using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.SerialOutput
{
    [TestFixture]
    public class TestXtermSanitizingReader
    {
        private class EnumerationReader<T> : IAsyncReader<T> where T : class
        {
            private readonly IEnumerator<T> enumerator;

            public EnumerationReader(IEnumerable<T> e)
            {
                this.enumerator = e.GetEnumerator();
            }

            public void Dispose()
            {
                this.enumerator.Dispose();
            }

            public Task<T> ReadAsync(CancellationToken token)
            {
                if (!this.enumerator.MoveNext())
                {
                    return Task.FromResult<T>(null!);
                }
                else
                {
                    return Task.FromResult(this.enumerator.Current);
                }
            }
        }

        //----------------------------------------------------------------------
        // Read.
        //----------------------------------------------------------------------

        [Test]
        public async Task Read_WhenStreamContainsPlainTextOnly()
        {
            var input = new[]
            {
                "some text",
                "",
                " and more text"
            };

            var reader = new XtermSanitizingReader(new EnumerationReader<string>(input));

            Assert.AreEqual(
                "some text",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                "",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                " and more text",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
        }

        [Test]
        public async Task Read_WhenStreamContainsControlSequences()
        {
            var input = new[]
            {
                "\u001B[2Jsome text\u001B",
                "[2J\u001B[2J",
                " and more text\u001B[2J"
            };

            var reader = new XtermSanitizingReader(new EnumerationReader<string>(input));

            Assert.AreEqual(
                "some text\u001b",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                "[2J",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                " and more text",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
        }

        [Test]
        public async Task Read_WhenStreamContainsImproperlyTerminatedControlSequences()
        {
            var input = new[]
            {
                "\u001B[2Jsome text\u001B",
                "[2J\u001B[2J",
                "\u001b[01;01\u001b",
                "[01;01H and more text\u001B[2J"
            };

            var reader = new XtermSanitizingReader(new EnumerationReader<string>(input));

            Assert.AreEqual(
                "some text\u001b",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                "[2J",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                "\u001b[01;01\u001b",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
            Assert.AreEqual(
                "[01;01H and more text",
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
        }

        [Test]
        public async Task Read_WhenStreamContainsTruncatedControlSequences()
        {
            var input = new[]
            {
                "[2Jsome text\u001B"
            };

            var reader = new XtermSanitizingReader(new EnumerationReader<string>(input));

            Assert.AreEqual(
                input[0],
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
        }
    }
}

