//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Extensions.Activity.Views.SerialOutput;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Views.SerialOutput
{
    [TestFixture]
    public class TestAnsiTokenReader : FixtureBase
    {
        private readonly string[] AnsiSequences = new[]
        {
            "\u001B[20h", //Set new 12 mode", //LMN

            "\u001BN", //Set single shift 2", //SS2
            "\u001BO", //Set single shift 3", //SS3

            "\u001B[m", //Turn off character attributes", //SGR0
            "\u001B[0m", //Turn off character attributes", //SGR0
            "\u001B[1m", //Turn bold mode on", //SGR1
            "\u001B[2m", //Turn low intensity mode on", //SGR2
            "\u001B[4m", //Turn underline mode on", //SGR4
            "\u001B[5m", //Turn blinking mode on", //SGR5
            "\u001B[7m", //Turn reverse video on", //SGR7
            "\u001B[8m", //Turn invisible text mode on", //SGR8

            "\u001B[12;12r", //Set top and bottom 12s of a window", //DECSTBM

            "\u001B[12A", //Move cursor up n lines", //CUU
            "\u001B[12B", //Move cursor down n lines", //CUD
            "\u001B[12C", //Move cursor right n lines", //CUF
            "\u001B[12D", //Move cursor left n lines", //CUB
            "\u001B[H", //Move cursor to upper left corner", //cursorhome
            "\u001B[;H", //Move cursor to upper left corner", //cursorhome
            "\u001B[12;23H", //Move cursor to screen location v,h", //CUP
            "\u001B[f", //Move cursor to upper left corner", //hvhome
            "\u001B[;f", //Move cursor to upper left corner", //hvhome
            "\u001B[12;23f", //Move cursor to screen location v,h", //CUP
            "\u001BD", //Move/scroll window up one line", //IND
            "\u001BM", //Move/scroll window down one line", //RI
            "\u001BE", //Move to next line", //NEL

            "\u001BH", //Set a tab at the current column", //HTS
            "\u001B[g", //Clear a tab at the current column", //TBC
            "\u001B[0g", //Clear a tab at the current column", //TBC
            "\u001B[3g", //Clear all tabs", //TBC

            "\u001B[K", //Clear line from cursor right", //EL0
            "\u001B[0K", //Clear line from cursor right", //EL0
            "\u001B[1K", //Clear line from cursor left", //EL1
            "\u001B[2K", //Clear entire line", //EL2

            "\u001B[J", //Clear screen from cursor down", //ED0
            "\u001B[0J", //Clear screen from cursor down", //ED0
            "\u001B[1J", //Clear screen from cursor up", //ED1
            "\u001B[2J", //Clear entire screen", //ED2

            "\u001B[c", //Identify what terminal type", //DA
            "\u001B[0c", //Identify what terminal type (another)", //DA
            "\u001B[2;1y", //Confidence power up test", //DECTST
            "\u001B[2;2y", //Confidence loopback test", //DECTST
            "\u001B[2;9y", //Repeat power up test", //DECTST
            "\u001B[2;10y", //Repeat loopback test", //DECTST

            "\u001B[0q", //Turn off all four leds", //DECLL0
            "\u001B[1q", //Turn on LED #1", //DECLL1
            "\u001B[2q", //Turn on LED #2", //DECLL2
            "\u001B[3q", //Turn on LED #3", //DECLL3
            "\u001B[4q", //Turn on LED #4", //DECLL4
        };

        //---------------------------------------------------------------------
        // Stream boundary handling.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenInputIsNull_ThenReadReturnsNull()
        {
            var input = new string[] { null };
            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));
            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenInputIsEmpty_ThenReadReturnsEmptyEnumerable()
        {
            var input = new string[] { "" };
            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenInputIsPlainText_ThenReadReturnsSingleToken()
        {
            var input = new string[] { "sample" };
            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("sample", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenEscSequenceSplitOverTwoReads_ThenSecondReadReturnsCommand()
        {
            var input = new string[] {
                "text",
                AnsiTokenReader.Escape.ToString(),
                "N",
                "text"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("text", result.First().Value);

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Command, result.First().Type);
            Assert.AreEqual("N", result.First().Value);

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("text", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenEscSequenceSplitOverTwoReadsAtEnd_ThenSecondReadReturnsCommand()
        {
            var input = new string[] {
                AnsiTokenReader.Escape.ToString(),
                "N"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Command, result.First().Type);
            Assert.AreEqual("N", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenEscSequenceCutOffAtEnd_ThenReadPartIsReturnedAsText()
        {
            var input = new string[] {
                AnsiTokenReader.Escape.ToString(),
                "["
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("\u001B[", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenCsiSequenceSplitOverTwoReads_ThenSecondReadReturnsCommand()
        {
            var input = new string[] {
                "text",
                AnsiTokenReader.Escape.ToString(),
                "[2J",
                "text"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("text", result.First().Value);

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Command, result.First().Type);
            Assert.AreEqual("[2J", result.First().Value);

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Text, result.First().Type);
            Assert.AreEqual("text", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenCsiSequenceSplitOverTwoReadsAtEnd_ThenSecondReadReturnsCommand()
        {
            var input = new string[] {
                AnsiTokenReader.Escape.ToString(),
                "[2J"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(0, result.Count());

            result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(AnsiTextToken.TokenType.Command, result.First().Type);
            Assert.AreEqual("[2J", result.First().Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        //---------------------------------------------------------------------
        // Token alternation.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenEscSequenceEmbeddedInText_ThenIndividualTokensAreReturned()
        {
            var esc = AnsiTokenReader.Escape.ToString();
            var input = new string[] {
                $"a{esc}A{esc}Bb{esc}Cc"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = (await reader.ReadAsync(CancellationToken.None)).ToList();
            Assert.AreEqual(6, result.Count());

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[0].Type);
            Assert.AreEqual("a", result[0].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[1].Type);
            Assert.AreEqual("A", result[1].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[2].Type);
            Assert.AreEqual("B", result[2].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[3].Type);
            Assert.AreEqual("b", result[3].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[4].Type);
            Assert.AreEqual("C", result[4].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[5].Type);
            Assert.AreEqual("c", result[5].Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenEscSequenceEmbeddedAtEnd_ThenIndividualTokensAreReturned()
        {
            var esc = AnsiTokenReader.Escape.ToString();
            var input = new string[] {
                $"a{esc}N{esc}Ob{esc}N"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = (await reader.ReadAsync(CancellationToken.None)).ToList();
            Assert.AreEqual(5, result.Count());

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[0].Type);
            Assert.AreEqual("a", result[0].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[1].Type);
            Assert.AreEqual("N", result[1].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[2].Type);
            Assert.AreEqual("O", result[2].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[3].Type);
            Assert.AreEqual("b", result[3].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[4].Type);
            Assert.AreEqual("N", result[4].Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenCsiSequenceEmbeddedInText_ThenIndividualTokensAreReturned()
        {
            var esc = AnsiTokenReader.Escape.ToString();
            var input = new string[] {
                $"a{esc}[1A{esc}[1Bb{esc}[1Cc"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = (await reader.ReadAsync(CancellationToken.None)).ToList();
            Assert.AreEqual(6, result.Count());

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[0].Type);
            Assert.AreEqual("a", result[0].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[1].Type);
            Assert.AreEqual("[1A", result[1].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[2].Type);
            Assert.AreEqual("[1B", result[2].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[3].Type);
            Assert.AreEqual("b", result[3].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[4].Type);
            Assert.AreEqual("[1C", result[4].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[5].Type);
            Assert.AreEqual("c", result[5].Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhenCsiSequenceEmbeddedAtEnd_ThenIndividualTokensAreReturned()
        {
            var esc = AnsiTokenReader.Escape.ToString();
            var input = new string[] {
                $"a{esc}[1A{esc}[1Bb{esc}[1C"
            };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = (await reader.ReadAsync(CancellationToken.None)).ToList();
            Assert.AreEqual(5, result.Count());

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[0].Type);
            Assert.AreEqual("a", result[0].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[1].Type);
            Assert.AreEqual("[1A", result[1].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[2].Type);
            Assert.AreEqual("[1B", result[2].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Text, result[3].Type);
            Assert.AreEqual("b", result[3].Value);

            Assert.AreEqual(AnsiTextToken.TokenType.Command, result[4].Type);
            Assert.AreEqual("[1C", result[4].Value);

            Assert.IsNull(await reader.ReadAsync(CancellationToken.None));
        }

        [Test]
        public async Task WhePassedAllAnsiSequences_ThenAllSequencesAreDetected()
        {
            var input = new string[] { string.Join("", AnsiSequences) };

            var reader = new AnsiTokenReader(new EnumerationReader<string>(input));

            var result = await reader.ReadAsync(CancellationToken.None);
            Assert.AreEqual(AnsiSequences.Length, result.Count());
            Assert.IsTrue(result.All(t => t.Type == AnsiTextToken.TokenType.Command));
            CollectionAssert.AreEqual(
                AnsiSequences,
                result.Select(t => "\u001B" + t.Value));
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
