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

using Google.Solutions.Platform.IO;
using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Platform.Test.IO
{
    [TestFixture]
    public class TestAnonymousPipe
    {
        //---------------------------------------------------------------------
        // Handle.
        //---------------------------------------------------------------------

        [Test]
        public void Handle_WhenWriteSideClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);

                pipe.CloseWriteSide();

                Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);
            }
        }

        [Test]
        public void Handle_WhenReadSideClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);

                pipe.CloseReadSide();

                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
            }
        }

        //---------------------------------------------------------------------
        // Stream.
        //---------------------------------------------------------------------

        [Test]
        public void Stream_WhenWriteStreamClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);

                pipe.WriteSide.Close();

                Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);
            }
        }

        [Test]
        public void Stream_WhenReadStreamClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);

                pipe.ReadSide.Close();

                Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
                Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
            }
        }

        [Test]
        public void Stream_WhenDataWrittenToWriteSide_ThenDataCanBeReadFromReadSide()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsTrue(pipe.ReadSide.CanRead);
                Assert.That(pipe.WriteSide.CanRead, Is.False);

                Assert.That(pipe.ReadSide.CanWrite, Is.False);
                Assert.IsTrue(pipe.WriteSide.CanWrite);

                var data = Encoding.ASCII.GetBytes("test");
                pipe.WriteSide.Write(data, 0, data.Length);
                pipe.WriteSide.Flush();

                var buffer = new byte[data.Length];
                var bytesRead = pipe.ReadSide.Read(buffer, 0, buffer.Length);

                Assert.That(bytesRead, Is.EqualTo(data.Length));
                Assert.That(buffer, Is.EqualTo(data).AsCollection);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose()
        {
            var pipe = new AnonymousPipe();
            Assert.That(pipe.WriteSideHandle.IsClosed, Is.False);
            Assert.That(pipe.ReadSideHandle.IsClosed, Is.False);

            pipe.Dispose();

            Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
            Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
        }
    }
}
