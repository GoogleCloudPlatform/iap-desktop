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

using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Platform.Interop;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVirtualFileDataObject
    {
        //----------------------------------------------------------------------
        // GetData.
        //----------------------------------------------------------------------

        [Test]
        public void GetData_WhenFormatSupported()
        {
            var content = Encoding.ASCII.GetBytes("Test");
            using (var contentStream = new MemoryStream())
            {
                contentStream.Write(content, 0, content.Length);

                var dataObject = new VirtualFileDataObject(new[] {
                    new VirtualFileDataObject.Descriptor(
                        "file-1.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        () => contentStream),
                    new VirtualFileDataObject.Descriptor(
                        "file-2.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        () => contentStream),
                });

                Assert.IsInstanceOf<Stream>(dataObject.GetData(
                    ShellDataFormats.CFSTR_FILEDESCRIPTORW,
                    false));

                Assert.IsInstanceOf<Stream>(dataObject.GetData(
                    ShellDataFormats.CFSTR_FILECONTENTS,
                    false));
            }
        }

        [Test]
        public void GetData_WhenFormatNotSupported()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            Assert.IsNull(dataObject.GetData("Unsupported", false));
        }

        [Test]
        public void GetData_WhenDisposed()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());
            dataObject.Dispose();
            Assert.IsNull(dataObject.GetData(
                ShellDataFormats.CFSTR_FILEDESCRIPTORW,
                false));
        }

        [Test]
        public void GetData_ReturnsStreamThatGuaranteesFullReads()
        {
            var copiedStream = new Mock<Stream>();
            copiedStream
                .SetupGet(s => s.Length)
                .Returns(2);
            copiedStream
                .Setup(s => s.Read(It.IsAny<byte[]>(), 0, 2))
                .Returns(1);
            copiedStream
                .Setup(s => s.Read(It.IsAny<byte[]>(), 1, 1))
                .Returns(1);

            var dataObject = new VirtualFileDataObject(new[] {
                    new VirtualFileDataObject.Descriptor(
                        "file-1.txt",
                        2,
                        FileAttributes.Normal,
                        () => copiedStream.Object)
                });

            var stream = (Stream)dataObject.GetData(
                ShellDataFormats.CFSTR_FILECONTENTS,
                false);
            Assert.AreEqual(2, stream.Length);
            Assert.AreEqual(2, stream.Read(new byte[2], 0, 2));
        }

        //----------------------------------------------------------------------
        // StartOperation.
        //----------------------------------------------------------------------

        [Test]
        public void StartOperation_RaisesEvent()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            var eventRaised = false;
            dataObject.AsyncOperationStarted += (_, args) => eventRaised = true;
            dataObject.StartOperation(null);

            dataObject.InOperation(out var inOp);
            Assert.AreEqual(-1, inOp);

            Assert.IsTrue(eventRaised);
        }

        //----------------------------------------------------------------------
        // EndOperation.
        //----------------------------------------------------------------------

        [Test]
        public void EndOperation_WhenSucceeded_ThenRaisesEvent()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            var eventRaised = false;
            dataObject.AsyncOperationCompleted += (_, args) =>
            {
                eventRaised = true;
                Assert.IsTrue(args.Succeeded);
            };

            dataObject.StartOperation(null);
            dataObject.EndOperation(0, null, 0);

            Assert.IsTrue(eventRaised);
        }

        [Test]
        public void EndOperation_WhenFailed_ThenRaisesEvent()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            var eventRaised = false;
            dataObject.AsyncOperationCompleted += (_, args) =>
            {
                eventRaised = true;
                Assert.IsFalse(args.Succeeded);
                Assert.IsNotNull(args.Exception);
            };

            dataObject.StartOperation(null);
            dataObject.EndOperation((int)HRESULT.E_UNEXPECTED, null, 0);

            Assert.IsTrue(eventRaised);
        }

        //----------------------------------------------------------------------
        // IsOperationInProgress.
        //----------------------------------------------------------------------

        [Test]
        public void IsOperationInProgress()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            Assert.IsFalse(dataObject.IsOperationInProgress);

            dataObject.StartOperation(null);
            Assert.IsTrue(dataObject.IsOperationInProgress);

            dataObject.EndOperation(0, null, 0);
            Assert.IsFalse(dataObject.IsOperationInProgress);
        }
    }
}
