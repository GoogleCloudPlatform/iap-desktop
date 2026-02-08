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
using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using UCOMIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

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

            Assert.That(dataObject.GetData("Unsupported", false), Is.Null);
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

        //----------------------------------------------------------------------
        // GetData (COM).
        //----------------------------------------------------------------------

        [Test]
        public void ComGetData_WhenTymedUnsupported()
        {
            var dataObject = (UCOMIDataObject)new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            var e = Assert.Throws<COMException>(
                () => dataObject.GetData(
                    new FORMATETC()
                    {
                        tymed = TYMED.TYMED_ISTORAGE,
                        cfFormat = (short)DataFormats.GetFormat(
                            ShellDataFormats.CFSTR_FILECONTENTS).Id
                    },
                    out var _));
            Assert.That((HRESULT)e!.ErrorCode, Is.EqualTo(HRESULT.DV_E_TYMED));
        }

        [Test]
        public void ComGetData_WhenTymedIncludesStream()
        {
            var content = Encoding.ASCII.GetBytes("Test");
            using (var contentStream = new MemoryStream())
            {
                contentStream.Write(content, 0, content.Length);

                var dataObject = (UCOMIDataObject)new VirtualFileDataObject(new[] {
                    new VirtualFileDataObject.Descriptor(
                        "file-1.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        () => contentStream),
                });

                dataObject.GetData(
                    new System.Runtime.InteropServices.ComTypes.FORMATETC()
                    {
                        tymed = TYMED.TYMED_ISTREAM |
                            TYMED.TYMED_ISTORAGE,
                        cfFormat = (short)DataFormats.GetFormat(
                            ShellDataFormats.CFSTR_FILECONTENTS).Id
                    },
                    out var medium);

                Assert.That(
                    medium.tymed, Is.EqualTo(TYMED.TYMED_ISTREAM));
                Assert.That(
                    medium.unionmember, Is.Not.EqualTo(IntPtr.Zero));
            }

        }

        [Test]
        public void ComGetData_WhenTymedIncludesHGlobal()
        {
            var content = Encoding.ASCII.GetBytes("Test");
            using (var contentStream = new MemoryStream())
            {
                contentStream.Write(content, 0, content.Length);

                var dataObject = (UCOMIDataObject)new VirtualFileDataObject(new[] {
                    new VirtualFileDataObject.Descriptor(
                        "file-1.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        () => contentStream),
                });

                dataObject.GetData(
                    new System.Runtime.InteropServices.ComTypes.FORMATETC()
                    {
                        tymed = TYMED.TYMED_HGLOBAL |
                            TYMED.TYMED_ISTORAGE,
                        cfFormat = (short)DataFormats.GetFormat(
                            ShellDataFormats.CFSTR_FILECONTENTS).Id
                    },
                    out var medium);

                Assert.That(
                    medium.tymed, Is.EqualTo(TYMED.TYMED_HGLOBAL));
                Assert.That(
                    medium.unionmember, Is.Not.EqualTo(IntPtr.Zero));
            }
        }

        [Test]
        public void ComGetData_WhenOpeningStreamFails(
            [Values(TYMED.TYMED_HGLOBAL, TYMED.TYMED_ISTREAM)]
            TYMED tymed)
        {
            var content = Encoding.ASCII.GetBytes("Test");
            using (var contentStream = new MemoryStream())
            {
                contentStream.Write(content, 0, content.Length);

                var dataObject = (UCOMIDataObject)new VirtualFileDataObject(new[] {
                    new VirtualFileDataObject.Descriptor(
                        "file-1.txt",
                        (ulong)content.Length,
                        FileAttributes.Normal,
                        () => throw new UnauthorizedAccessException()),
                });

                Assert.Throws<UnauthorizedAccessException>(
                    () => dataObject.GetData(
                        new System.Runtime.InteropServices.ComTypes.FORMATETC()
                        {
                            tymed = tymed,
                            cfFormat = (short)DataFormats.GetFormat(
                                ShellDataFormats.CFSTR_FILECONTENTS).Id
                        },
                        out var medium));
            }
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
            Assert.That(inOp, Is.EqualTo(-1));

            Assert.That(eventRaised, Is.True);
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
                Assert.That(args.Succeeded, Is.True);
            };

            dataObject.StartOperation(null);
            dataObject.EndOperation(0, null, 0);

            Assert.That(eventRaised, Is.True);
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
                Assert.That(args.Succeeded, Is.False);
                Assert.That(args.Exception, Is.Not.Null);
            };

            dataObject.StartOperation(null);
            dataObject.EndOperation((int)HRESULT.E_UNEXPECTED, null, 0);

            Assert.That(eventRaised, Is.True);
        }

        //----------------------------------------------------------------------
        // IsOperationInProgress.
        //----------------------------------------------------------------------

        [Test]
        public void IsOperationInProgress()
        {
            var dataObject = new VirtualFileDataObject(
                Array.Empty<VirtualFileDataObject.Descriptor>());

            Assert.That(dataObject.IsOperationInProgress, Is.False);

            dataObject.StartOperation(null);
            Assert.That(dataObject.IsOperationInProgress, Is.True);

            dataObject.EndOperation(0, null, 0);
            Assert.That(dataObject.IsOperationInProgress, Is.False);
        }
    }
}
