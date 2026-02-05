//
// Copyright 2025 Google LLC
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

using Google.Solutions.Common.Interop;
using Google.Solutions.Mvvm.Interop;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Google.Solutions.Mvvm.Test.Interop
{
    [TestFixture]
    public class TestComStream
    {
        //--------------------------------------------------------------------
        // Read.
        //--------------------------------------------------------------------

        [Test]
        public void Read_WhenDataAvailable()
        {
            using (var bytesRead = GlobalAllocSafeHandle.GlobalAlloc((uint)IntPtr.Size))
            using (var stream = new ComStream(
                new MemoryStream(Encoding.ASCII.GetBytes("abcd"))))
            {
                var istream = (IStream)stream;

                var buffer = new byte[16];
                istream.Read(buffer, buffer.Length, bytesRead.DangerousGetHandle());

                Assert.That(Marshal.ReadInt32(bytesRead.DangerousGetHandle()), Is.EqualTo(4));
            }
        }

        [Test]
        public void Read_WhenAtEnd()
        {
            using (var bytesRead = GlobalAllocSafeHandle.GlobalAlloc((uint)IntPtr.Size))
            using (var stream = new ComStream(new MemoryStream(Array.Empty<byte>())))
            {
                var istream = (IStream)stream;

                var buffer = new byte[16];
                istream.Read(buffer, buffer.Length, bytesRead.DangerousGetHandle());

                Assert.That(Marshal.ReadInt32(bytesRead.DangerousGetHandle()), Is.EqualTo(0));
            }
        }

        [Test]
        public void Read_WhenSpeculatedSeekSucceeds()
        {
            var nonSeekableStream = new Mock<Stream>();
            nonSeekableStream.SetupGet(s => s.Position).Returns(0);
            nonSeekableStream.SetupGet(s => s.CanSeek).Returns(false);

            using (var stream = new ComStream(nonSeekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(0, ComStream.STREAM_SEEK_SET, IntPtr.Zero);

                var buffer = new byte[16];
                istream.Read(buffer, buffer.Length, IntPtr.Zero);
            }
        }

        [Test]
        public void Read_WhenSpeculatedSeekFails()
        {
            var nonSeekableStream = new Mock<Stream>();
            nonSeekableStream.SetupGet(s => s.Position).Returns(1);
            nonSeekableStream.SetupGet(s => s.CanSeek).Returns(false);

            using (var stream = new ComStream(nonSeekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(0, ComStream.STREAM_SEEK_SET, IntPtr.Zero);

                var buffer = new byte[16];
                Assert.Throws<NotSupportedException>(
                    () => istream.Read(buffer, buffer.Length, IntPtr.Zero));
            }
        }

        //--------------------------------------------------------------------
        // Write.
        //--------------------------------------------------------------------

        [Test]
        public void Write()
        {
            using (var bytesWritten = GlobalAllocSafeHandle.GlobalAlloc((uint)IntPtr.Size))
            using (var stream = new ComStream(new MemoryStream()))
            {
                var istream = (IStream)stream;

                var buffer = Encoding.ASCII.GetBytes("abcd");
                istream.Write(buffer, 4, bytesWritten.DangerousGetHandle());

                Assert.That(Marshal.ReadInt32(bytesWritten.DangerousGetHandle()), Is.EqualTo(4));
            }
        }

        [Test]
        public void Write_WhenSpeculatedSeekSucceeds()
        {
            var nonSeekableStream = new Mock<Stream>();
            nonSeekableStream.SetupGet(s => s.Position).Returns(0);
            nonSeekableStream.SetupGet(s => s.CanSeek).Returns(false);

            using (var stream = new ComStream(nonSeekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(0, ComStream.STREAM_SEEK_SET, IntPtr.Zero);

                var buffer = Encoding.ASCII.GetBytes("abcd");
                istream.Write(buffer, 4, IntPtr.Zero);
            }
        }

        [Test]
        public void Write_WhenSpeculatedSeekFails()
        {
            var nonSeekableStream = new Mock<Stream>();
            nonSeekableStream.SetupGet(s => s.Position).Returns(1);
            nonSeekableStream.SetupGet(s => s.CanSeek).Returns(false);

            using (var stream = new ComStream(nonSeekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(2, ComStream.STREAM_SEEK_CUR, IntPtr.Zero);

                var buffer = Encoding.ASCII.GetBytes("abcd");
                Assert.Throws<NotSupportedException>(
                    () => istream.Write(buffer, 4, IntPtr.Zero));
            }
        }

        //--------------------------------------------------------------------
        // Seek - w/o speculation.
        //--------------------------------------------------------------------

        [Test]
        public void Seek_Set_WhenStreamCanSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(true);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(4, ComStream.STREAM_SEEK_SET, IntPtr.Zero);

                Assert.IsNull(stream.SpeculatedPosition);
            }

            seekableStream.Verify(s => s.Seek(4, SeekOrigin.Begin));
        }

        [Test]
        public void Seek_Cur_WhenStreamCanSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(true);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(4, ComStream.STREAM_SEEK_CUR, IntPtr.Zero);

                Assert.IsNull(stream.SpeculatedPosition);
            }

            seekableStream.Verify(s => s.Seek(4, SeekOrigin.Current));
        }

        [Test]
        public void Seek_End_WhenStreamCanSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(true);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(4, ComStream.STREAM_SEEK_END, IntPtr.Zero);

                Assert.IsNull(stream.SpeculatedPosition);
            }

            seekableStream.Verify(s => s.Seek(4, SeekOrigin.End));
        }

        //--------------------------------------------------------------------
        // Seek - w/ speculation.
        //--------------------------------------------------------------------

        [Test]
        public void Seek_Set_WhenStreamCannotSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(false);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(4, ComStream.STREAM_SEEK_SET, IntPtr.Zero);
                Assert.That(stream.SpeculatedPosition, Is.EqualTo(4));
            }
        }

        [Test]
        public void Seek_Cur_WhenStreamCannotSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(false);
            seekableStream.SetupGet(s => s.Position).Returns(4);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(-2, ComStream.STREAM_SEEK_CUR, IntPtr.Zero);
                Assert.That(stream.SpeculatedPosition, Is.EqualTo(2));
            }
        }

        [Test]
        public void Seek_End_WhenStreamCannotSeek()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanSeek).Returns(false);
            seekableStream.SetupGet(s => s.Position).Returns(4);
            seekableStream.SetupGet(s => s.Length).Returns(8);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Seek(-2, ComStream.STREAM_SEEK_END, IntPtr.Zero);
                Assert.That(stream.SpeculatedPosition, Is.EqualTo(6));
            }
        }

        //--------------------------------------------------------------------
        // Stat.
        //--------------------------------------------------------------------

        [Test]
        public void Stat_WhenReadOnly()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanRead).Returns(true);
            seekableStream.SetupGet(s => s.CanWrite).Returns(false);
            seekableStream.SetupGet(s => s.Length).Returns(8);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Stat(out var stat, 0);

                Assert.That(stat.grfMode, Is.EqualTo(ComStream.STGM_READ));
                Assert.That(stat.cbSize, Is.EqualTo(8));
                Assert.That(stat.type, Is.EqualTo(ComStream.STGTY_STREAM));
            }
        }

        [Test]
        public void Stat_WhenWriteOnly()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanRead).Returns(false);
            seekableStream.SetupGet(s => s.CanWrite).Returns(true);
            seekableStream.SetupGet(s => s.Length).Returns(8);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Stat(out var stat, 0);

                Assert.That(stat.grfMode, Is.EqualTo(ComStream.STGM_WRITE));
                Assert.That(stat.cbSize, Is.EqualTo(8));
                Assert.That(stat.type, Is.EqualTo(ComStream.STGTY_STREAM));
            }
        }

        [Test]
        public void Stat_WhenReadWrite()
        {
            var seekableStream = new Mock<Stream>();
            seekableStream.SetupGet(s => s.CanRead).Returns(true);
            seekableStream.SetupGet(s => s.CanWrite).Returns(true);
            seekableStream.SetupGet(s => s.Length).Returns(8);

            using (var stream = new ComStream(seekableStream.Object))
            {
                var istream = (IStream)stream;

                istream.Stat(out var stat, 0);

                Assert.That(stat.grfMode, Is.EqualTo(ComStream.STGM_READWRITE));
                Assert.That(stat.cbSize, Is.EqualTo(8));
                Assert.That(stat.type, Is.EqualTo(ComStream.STGTY_STREAM));
            }
        }
    }
}
