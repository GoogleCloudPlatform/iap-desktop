//
// Copyright 2024 Google LLC
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


using Google.Solutions.Common.IO;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.IO
{
    [TestFixture]
    public class TestStreamExtensions
    {
        //--------------------------------------------------------------------
        // CopyTo.
        //--------------------------------------------------------------------

        [Test]
        public void CopyTo_WhenSourceNotReadable()
        {
            var sourceStream = new Mock<Stream>();
            sourceStream.SetupGet(s => s.CanRead).Returns(false);

            Assert.Throws<NotSupportedException>(
                () => sourceStream.Object.CopyTo(
                new MemoryStream(),
                new Mock<IProgress<int>>().Object));
        }

        [Test]
        public void CopyTo_WhenDestinationNotWritable()
        {
            var destinationStream = new Mock<Stream>();
            destinationStream.SetupGet(s => s.CanWrite).Returns(false);

            Assert.Throws<NotSupportedException>(
                () => new MemoryStream().CopyTo(
                destinationStream.Object,
                new Mock<IProgress<int>>().Object));
        }

        [Test]
        public void CopyTo_ReportsProgress()
        {
            var source = new byte[StreamExtensions.DefaultBufferSize * 3 + 1];
            var sourceStream = new MemoryStream(source);

            var progress = new Mock<IProgress<int>>();
            sourceStream.CopyTo(
                new MemoryStream(),
                progress.Object);

            progress.Verify(p => p.Report(It.IsAny<int>()), Times.AtLeast(4));
        }

        //--------------------------------------------------------------------
        // CopyToAsync.
        //--------------------------------------------------------------------

        [Test]
        public async Task CopyToAsync_WhenSourceNotReadable()
        {
            var sourceStream = new Mock<Stream>();
            sourceStream.SetupGet(s => s.CanRead).Returns(false);

            await ExceptionAssert
                .ThrowsAsync<NotSupportedException>(() => sourceStream.Object.CopyToAsync(
                    new MemoryStream(),
                    new Mock<IProgress<int>>().Object,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToAsync_WhenDestinationNotWritable()
        {
            var destinationStream = new Mock<Stream>();
            destinationStream.SetupGet(s => s.CanWrite).Returns(false);

            await ExceptionAssert
                .ThrowsAsync<NotSupportedException>(() => new MemoryStream().CopyToAsync(
                    destinationStream.Object,
                    new Mock<IProgress<int>>().Object,
                    CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CopyToAsync_ReportsProgress()
        {
            var source = new byte[StreamExtensions.DefaultBufferSize * 3 + 1];
            var sourceStream = new MemoryStream(source);

            var progress = new Mock<IProgress<int>>();
            await sourceStream
                .CopyToAsync(
                    new MemoryStream(),
                    progress.Object,
                    CancellationToken.None)
                .ConfigureAwait(false);

            progress.Verify(p => p.Report(It.IsAny<int>()), Times.AtLeast(4));
        }
    }
}
