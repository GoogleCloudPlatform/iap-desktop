using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.SerialOutput
{
    [TestFixture]
    public class TestAnsiTextReader : FixtureBase
    {
        [Test]
        public async Task WhenStreamContainsPlainTextOnly_ThenTextIsReturnedVerbatim()
        {
            var input = new[]
            {
                "some text",
                "",
                " and more text"
            };

            var reader = new AnsiTextReader(new EnumerationReader<string>(input));

            Assert.AreEqual("some text", await reader.ReadAsync(CancellationToken.None));
            Assert.AreEqual("", await reader.ReadAsync(CancellationToken.None));
            Assert.AreEqual(" and more text", await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenStreamContainsAnsiTokens_ThenTokensAreFilteredOut()
        {
            var input = new[]
            {
                "\u001B[2Jsome text\u001B",
                "[2J\u001B[2J",
                " and more text\u001B[2J"
            };

            var reader = new AnsiTextReader(new EnumerationReader<string>(input));

            Assert.AreEqual("some text", await reader.ReadAsync(CancellationToken.None));
            Assert.AreEqual("", await reader.ReadAsync(CancellationToken.None));
            Assert.AreEqual(" and more text", await reader.ReadAsync(CancellationToken.None));
        }

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
                    return Task.FromResult<T>(null);
                }
                else
                {
                    return Task.FromResult(this.enumerator.Current);
                }
            }
        }
    }
}
