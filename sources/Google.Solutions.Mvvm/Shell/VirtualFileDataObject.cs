﻿//
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

using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Interop;
using Google.Solutions.Platform.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using UCOMIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Google.Solutions.Mvvm.Shell
{
    /// <summary>
    /// IDataObject that allows handling mutiple virtual files in a 
    /// single operation.
    /// </summary>
    /// <remarks>
    /// DataObject only supports handling a single virtual file 
    /// (using CFSTR_FILEDESCRIPTORW, CFSTR_FILECONTENTS). 
    /// 
    /// This class extends DataObject to support multiple files, inspired by
    /// https://www.codeproject.com/Articles/23139/Transferring-Virtual-Files-to-Windows-Explorer-in
    /// </remarks>
    public sealed class VirtualFileDataObject
        : DataObject, UCOMIDataObject, VirtualFileDataObject.IDataObjectAsyncCapability, IDisposable
    {
        private int currentFile = 0;
        private bool disposed;

        /// <summary>
        /// List of streams that were opened during the last operation.
        /// </summary>
        private readonly List<Stream> openedContentStreams = new List<Stream>();

        /// <summary>
        /// List of virtual files.
        /// </summary>
        public IList<Descriptor> Files { get; }

        public VirtualFileDataObject(IList<Descriptor> files)
        {
            this.Files = files;

            //
            // Enable delayed rendering
            //
            SetData(ShellDataFormats.CFSTR_FILEDESCRIPTORW, null);
            SetData(ShellDataFormats.CFSTR_FILECONTENTS, null);
            SetData(ShellDataFormats.CFSTR_PERFORMEDDROPEFFECT, null);
        }

        /// <summary>
        /// Indicates if data extraction should be done asynchronously, i.e.,
        /// on a background thrad.
        /// </summary>
        internal bool IsAsync { get; set; }

        /// <summary>
        /// Indicates that data extraction is ongoing.
        /// </summary>
        internal bool IsOperationInProgress { get; private set; }

        /// <summary>
        /// Raised when asynchronous data extraction is starting.
        /// </summary>
        public event EventHandler? AsyncOperationStarted;

        /// <summary>
        /// Raised when asynchronous data extraction ahs ended.
        /// </summary>
        public event EventHandler<AsyncOperationEventArgs>? AsyncOperationCompleted;

        /// <summary>
        /// Raised when asynchronous reading or writing failed.
        /// </summary>
        public event EventHandler<ExceptionEventArgs>? AsyncOperationFailed;

        private void ExpectNotDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        //----------------------------------------------------------------------
        // DataObject/UCOMIDataObject overrides.
        //----------------------------------------------------------------------

        public override object? GetData(string format, bool autoConvert)
        {
            if (this.disposed)
            {
                return null;
            }

            if (ShellDataFormats.CFSTR_FILEDESCRIPTORW.Equals(format, StringComparison.OrdinalIgnoreCase))
            {
                //
                // Supply group descriptor for all files.
                //
                base.SetData(
                    ShellDataFormats.CFSTR_FILEDESCRIPTORW,
                    Descriptor.ToNativeGroupDescriptorStream(this.Files));
            }
            else if (ShellDataFormats.CFSTR_FILECONTENTS.Equals(format, StringComparison.OrdinalIgnoreCase))
            {
                //
                // Open the stream. We do that lazily in case the client
                // never actally invokes this method.
                //
                // NB. The base class doesn't dispose the stream. So we need
                //     to keep track of it and dispose it once the operation
                //     has completed.
                //

                if (this.currentFile >= 0 && this.currentFile < this.Files.Count)
                {
                    var contentStream = this.Files[this.currentFile].OpenStream();
                    this.openedContentStreams.Add(contentStream);

                    //
                    // Supply data for the current file.
                    //
                    base.SetData(
                        ShellDataFormats.CFSTR_FILECONTENTS,
                        contentStream);
                }
                else
                {
                    //
                    // Index out of range.
                    //
                    base.SetData(
                        ShellDataFormats.CFSTR_FILECONTENTS,
                        null);
                }
            }

            return base.GetData(format, autoConvert);
        }

        /// <summary>
        /// Get data to drop.
        /// 
        /// NB. This method differs in 2 ways from the base class implementation:
        ///
        ///     1. It extracts the index of the file that is currently
        ///        being processed. This
        ///     2. It adds support for TYMED_ISTREAM.
        /// </summary>
        void UCOMIDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
        {
            if (this.disposed)
            {
                medium = default;
                medium.tymed = TYMED.TYMED_NULL;
                return;
            }

            if (formatetc.cfFormat ==
                (short)DataFormats.GetFormat(ShellDataFormats.CFSTR_FILECONTENTS).Id)
            {
                //
                // Cache the index so that we can use it in GetData(format, autoConvert).
                //
                this.currentFile = formatetc.lindex;
            }

            //
            // Populate the medium.
            //
            medium = default;
            if (GetTymedUseable(formatetc.tymed))
            {
                var formatName = DataFormats.GetFormat(formatetc.cfFormat).Name;
                this.IsOperationInProgress = true;

                try
                {
                    if ((formatetc.tymed & TYMED.TYMED_ISTREAM) != 0 &&
                        GetDataPresent(formatName) &&
                        GetData(formatName, false) is Stream dataStream)
                    {
                        //
                        // Return data as a COM IStream.
                        //
                        var streamPtr = Marshal.GetIUnknownForObject(new ComStream(dataStream));
                    
                        medium.tymed = TYMED.TYMED_ISTREAM;
                        medium.unionmember = streamPtr;
                    }
                    else if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != 0)
                    {
                        //
                        // Return data as an HGLOBAL. The base class can do
                        // that for us.
                        //
                        medium.tymed = TYMED.TYMED_HGLOBAL;
                        medium.unionmember = NativeMethods.GlobalAlloc(GHND | GMEM_DDESHARE, 1);
                        if (medium.unionmember == IntPtr.Zero)
                        {
                            throw new OutOfMemoryException();
                        }

                        try
                        {
                            //
                            // Copy data. This will invoke GetData(format, autoConvert), which
                            // in turn uses the cached index to provide the right data.
                            //

                            ((UCOMIDataObject)this).GetDataHere(ref formatetc, ref medium);
                        }
                        catch (Exception)
                        {
                            NativeMethods.GlobalFree(new HandleRef(medium, medium.unionmember));
                            medium.unionmember = IntPtr.Zero;

                            throw;
                        }
                    }
                    else
                    {
                        medium.tymed = formatetc.tymed;
                        ((UCOMIDataObject)this).GetDataHere(ref formatetc, ref medium);
                    }
                }
                catch (Exception e)
                {
                    if (e is COMException comEx && (
                        comEx.HResult == (int)HRESULT.DV_E_FORMATETC ||
                        comEx.HResult == (int)HRESULT.E_FAIL))
                    {
                        //
                        // These can happen during format negotiation and 
                        // aren't worth raising an event for.
                        //
                    }
                    else if (this.IsAsync)
                    {
                        this.AsyncOperationFailed?.Invoke(this, new ExceptionEventArgs(e));
                    }

                    throw;
                }
            }
            else
            {
                Marshal.ThrowExceptionForHR((int)HRESULT.DV_E_TYMED);
            }

            bool GetTymedUseable(TYMED tymed)
            {
                var allowed = new TYMED[5]
                {
                    TYMED.TYMED_HGLOBAL,
                    TYMED.TYMED_ISTREAM,
                    TYMED.TYMED_ENHMF,
                    TYMED.TYMED_MFPICT,
                    TYMED.TYMED_GDI
                };

                for (var i = 0; i < allowed.Length; i++)
                {
                    if ((tymed & allowed[i]) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        //----------------------------------------------------------------------
        // IDataObjectAsyncCapability.
        //----------------------------------------------------------------------

        public void SetAsyncMode([In] int fDoOpAsync)
        {
            ExpectNotDisposed();

            this.IsAsync = fDoOpAsync != VariantBool.False;
        }

        public void GetAsyncMode([Out] out int pfIsOpAsync)
        {
            ExpectNotDisposed();

            pfIsOpAsync = this.IsAsync ? VariantBool.True : VariantBool.False;
        }

        public void StartOperation([In] IBindCtx? pbcReserved)
        {
            ExpectNotDisposed();

            this.IsOperationInProgress = true;
            this.AsyncOperationStarted?.Invoke(this, EventArgs.Empty);
        }

        public void EndOperation([In] int hResult, [In] IBindCtx? pbcReserved, [In] uint dwEffects)
        {
            ExpectNotDisposed();
            Precondition.Expect(this.IsOperationInProgress, "Operation not started");

            //
            // Close all streams that were opened.
            //
            foreach (var stream in this.openedContentStreams)
            {
                stream.Dispose();
            }

            this.IsOperationInProgress = false;
            this.AsyncOperationCompleted?.Invoke(
                this,
                ((HRESULT)hResult).Succeeded()
                    ? new AsyncOperationEventArgs(null)
                    : new AsyncOperationEventArgs(Marshal.GetExceptionForHR(hResult)));
        }

        public void InOperation([Out] out int pfInAsyncOp)
        {
            ExpectNotDisposed();

            pfInAsyncOp = this.IsOperationInProgress ? VariantBool.True : VariantBool.False;
        }

        //----------------------------------------------------------------------
        // IDisposable.
        //----------------------------------------------------------------------

        public void Dispose()
        {
            foreach (var stream in this.openedContentStreams)
            {
                stream.Dispose();
            }

            this.disposed = true;
        }

        //----------------------------------------------------------------------
        // Inner types.
        //----------------------------------------------------------------------

        public delegate Stream OpenStreamDelegate();

        /// <summary>
        /// Represents a virtual file and its metadata.
        /// </summary>
        public class Descriptor
        {
            private readonly OpenStreamDelegate openContentStream;

            /// <summary>
            /// Create a new descriptor.
            /// </summary>
            /// <remarks>
            /// When the data object is disposed, all opened streams
            /// are disposed automatically.
            /// </remarks>
            public Descriptor(
                string name,
                ulong size,
                FileAttributes attributes,
                OpenStreamDelegate openContentStream)
            {
                Precondition.Expect(
                    !name.Contains("\\"),
                    "Name must not contain a path separator");

                this.Name = name;
                this.Size = size;
                this.Attributes = attributes;
                this.openContentStream = openContentStream;
            }

            /// <summary>
            /// File size.
            /// </summary>
            public ulong Size { get; }

            /// <summary>
            /// File name, without path.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// File attributes.
            /// </summary>
            public FileAttributes Attributes { get; }

            /// <summary>
            /// File creation time.
            /// </summary>
            public DateTime? CreationTime { get; set; }

            /// <summary>
            /// Last access time.
            /// </summary>
            public DateTime? LastAccessTime { get; set; }

            /// <summary>
            /// Last change time.
            /// </summary>
            public DateTime? LastWriteTime { get; set; }

            /// <summary>
            /// Open the stream that contains the file contents.
            /// </summary>
            internal Stream OpenStream()
            {
                return this.openContentStream();
            }

            /// <summary>
            /// Convert to FILEDESCRIPTORW struct. 
            /// </summary>
            internal FILEDESCRIPTORW ToNativeFileDescriptor(bool requireProgressUi)
            {
                var native = new FILEDESCRIPTORW()
                {
                    dwFlags =
                        FD_FILESIZE |
                        FD_UNICODE |
                        (requireProgressUi ? FD_PROGRESSUI : 0),
                    cFileName = this.Name,
                    dwFileAttributes = (uint)this.Attributes,
                    nFileSizeHigh = (uint)(this.Size >> 32),
                    nFileSizeLow = (uint)(this.Size & 0xFFFFFFFF),
                };

                if (this.CreationTime != null)
                {
                    native.dwFlags |= FD_CREATETIME;
                    native.ftCreationTime = FileTimeFromDateTime(this.CreationTime.Value);
                }

                if (this.LastAccessTime != null)
                {
                    native.dwFlags |= FD_ACCESSTIME;
                    native.ftCreationTime = FileTimeFromDateTime(this.LastAccessTime.Value);
                }

                if (this.LastWriteTime != null)
                {
                    native.dwFlags |= FD_WRITESTIME;
                    native.ftCreationTime = FileTimeFromDateTime(this.LastWriteTime.Value);
                }

                return native;
            }

            /// <summary>
            /// Convert to a FILEGROUPDESCRIPTORW, wrapped in a stream.
            /// </summary>
            internal static MemoryStream ToNativeGroupDescriptorStream(
                IList<Descriptor> fileDescriptors)
            {
                //
                // FILEGROUPDESCRIPTORW is a variabe-length struct,
                // so we write it member by member.
                //
                // 1. Write cItems.
                //
                var stream = new MemoryStream();
                stream.Write(BitConverter.GetBytes(fileDescriptors.Count), 0, sizeof(uint));

                //
                // 2. Write fgd[..].
                //
                var structSize = Marshal.SizeOf<FILEDESCRIPTORW>();
                using (var ptr = GlobalAllocSafeHandle.GlobalAlloc((uint)structSize))
                {
                    var buffer = new byte[structSize];

                    for (var i = 0; i < fileDescriptors.Count; i++)
                    {
                        Marshal.StructureToPtr(
                            fileDescriptors[i].ToNativeFileDescriptor(true),
                            ptr.DangerousGetHandle(),
                            false);

                        Marshal.Copy(ptr.DangerousGetHandle(), buffer, 0, structSize);
                        stream.Write(buffer, 0, buffer.Length);
                    }

                    return stream;
                }
            }

            /// <summary>
            /// Convert DateTime to a FILETIME.
            /// </summary>
            private static System.Runtime.InteropServices.ComTypes.FILETIME FileTimeFromDateTime(
                DateTime dt)
            {
                var utc = dt.ToFileTimeUtc();

                return new System.Runtime.InteropServices.ComTypes.FILETIME()
                {
                    dwHighDateTime = (int)(utc >> 32),
                    dwLowDateTime = (int)(utc & 0xFFFFFFFF),
                };
            }
        }

        public class AsyncOperationEventArgs : EventArgs
        {
            public Exception? Exception { get; }

            public bool Succeeded
            {
                get => this.Exception == null;
            }

            internal AsyncOperationEventArgs(Exception? exception)
            {
                this.Exception = exception;
            }
        }

        //----------------------------------------------------------------------
        // Interop declarations.
        //----------------------------------------------------------------------

        private const uint FD_CREATETIME = 0x00000008;
        private const uint FD_ACCESSTIME = 0x00000010;
        private const uint FD_WRITESTIME = 0x00000020;
        private const uint FD_FILESIZE = 0x00000040;
        private const uint FD_PROGRESSUI = 0x00004000;
        private const uint FD_UNICODE = 0x80000000;

        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const uint GHND = (GMEM_MOVEABLE | GMEM_ZEROINIT);
        private const uint GMEM_DDESHARE = 0x2000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct FILEDESCRIPTORW
        {
            public uint dwFlags;

            /// <summary>
            /// The file type identifier.
            /// </summary>
            public Guid clsid;

            /// <summary>
            /// The width and height of the file icon.
            /// </summary>
            public System.Drawing.Size sizel;

            /// <summary>
            /// The screen coordinates of the file object.
            /// </summary>
            public System.Drawing.Point pointl;

            /// <summary>
            /// File attribute flags, in FILE_ATTRIBUTE_ format.
            /// </summary>
            public uint dwFileAttributes;

            /// <summary>
            /// The FILETIME structure that contains the time that the file was last accessed.
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;

            /// <summary>
            /// The FILETIME structure that contains the time that the file was last accessed.
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;

            /// <summary>
            /// The FILETIME structure that contains the time of the last write operation.
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;

            /// <summary>
            /// The high-order DWORD of the file size, in bytes.
            /// </summary>
            public uint nFileSizeHigh;

            /// <summary>
            /// The low-order DWORD of the file size, in bytes.
            /// </summary>
            public uint nFileSizeLow;

            /// <summary>
            /// The null-terminated string that contains the name of the file.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", ExactSpelling = true)]
            public static extern IntPtr GlobalAlloc(uint uFlags, int dwBytes);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            public static extern IntPtr GlobalFree(HandleRef handle);
        }

        /// <summary>
        /// Definition of the IDataObjectAsyncCapability (formerly named IAsyncOperation)
        /// COM interface.
        /// </summary>
        [ComImport]
        [Guid("3D8B0590-F691-11d2-8EA9-006097DF5BD4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDataObjectAsyncCapability
        {
            /// <summary>
            /// Called by a drop source to specify whether the data object 
            /// supports asynchronous data extraction.
            /// </summary>
            void SetAsyncMode([In] int fDoOpAsync);

            /// <summary>
            /// Called by a drop target to determine whether the data object 
            /// supports asynchronous data extraction.
            /// </summary>
            void GetAsyncMode([Out] out int pfIsOpAsync);

            /// <summary>
            /// Called by a drop target to indicate that asynchronous 
            /// data extraction is starting.
            /// </summary>
            /// <param name="pbcReserved"></param>
            void StartOperation([In] IBindCtx? pbcReserved);

            /// <summary>
            /// Called by the drop source to determine whether the target 
            /// is extracting data asynchronously.
            /// </summary>
            void InOperation([Out] out int pfInAsyncOp);

            /// <summary>
            /// Notifies the data object that the asynchronous data 
            /// extraction has ended.
            /// </summary>
            void EndOperation(
                [In] int hResult,
                [In] IBindCtx? pbcReserved,
                [In] uint dwEffects);
        }
    }
}
