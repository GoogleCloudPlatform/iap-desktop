﻿//
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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Views.SerialOutput
{
    [TestFixture]
    public class TestAnsiTextReader : ActivityFixtureBase
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
        public async Task WhenStreamContainsAnsiTokens_ThenTokensAreFilteredOut()
        {
            var input = new[]
            {
                "\u001B[2Jsome text\u001B",
                "[2J\u001B[2J",
                " and more text\u001B[2J"
            };

            var reader = new AnsiTextReader(new EnumerationReader<string>(input));

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
        public async Task WhenStreamContainsImproperlyTerminatedTokens_ThenTokensAreFilteredOut()
        {
            var input = new[]
            {
                "\u001B[2Jsome text\u001B",
                "[2J\u001B[2J",
                "\u001b[01;01\u001b",
                "[01;01H and more text\u001B[2J"
            };

            var reader = new AnsiTextReader(new EnumerationReader<string>(input));

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
        public async Task WhenStreamContainsTruncatedTokens_ThenTokensAreFilteredOut()
        {
            var input = new[]
            {
                "[2Jsome text\u001B"
            };

            var reader = new AnsiTextReader(new EnumerationReader<string>(input));

            Assert.AreEqual(
                "[2Jsome text", 
                await reader
                    .ReadAsync(CancellationToken.None)
                    .ConfigureAwait(false));
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
