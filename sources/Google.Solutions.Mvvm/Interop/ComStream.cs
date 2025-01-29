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

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Google.Solutions.Mvvm.Interop
{
    /// <summary>
    /// Wraps a managed stream as a COM IStream.
    /// </summary>
    internal sealed class ComStream : IStream, IDisposable
    {
        private readonly Stream stream;

        public ComStream(Stream stream)
        {
            this.stream = stream;
        }

        internal long? SpeculatedPosition { get; private set; } = null;
        
        /// <summary>
        /// Verify and retire a speculation that a previous seek was
        /// unnecessary.
        /// </summary>
        private void RetireSpeculatedSeek()
        {
            if (this.SpeculatedPosition != null &&
                this.SpeculatedPosition != this.stream.Position)
            {
                throw new NotSupportedException(
                    "The stream does not support seeking");
            }

            this.SpeculatedPosition = null;
        }

        //--------------------------------------------------------------------
        // IStream.
        //--------------------------------------------------------------------

        /// <summary>
        /// Reads a specified number of bytes from the stream object into memory
        /// starting at the current seek pointer.
        /// </summary>
        void IStream.Read(byte[] buffer, int count, IntPtr bytesReadPtr)
        {
            RetireSpeculatedSeek();

            var bytesRead = this.stream.Read(buffer, 0, (int)count);
            if (bytesReadPtr != IntPtr.Zero)
            {
                Marshal.WriteInt32(bytesReadPtr, bytesRead);
            }
        }

        /// <summary>
        /// Writes a specified number of bytes into the stream object starting 
        /// at the current seek pointer.
        /// </summary>
        void IStream.Write(byte[] buffer, int count, IntPtr bytesWrittenPtr)
        {
            RetireSpeculatedSeek();

            this.stream.Write(buffer, 0, count);
            if (bytesWrittenPtr != IntPtr.Zero)
            {
                // 
                // It's safe to assume that we wrote the entire buffer.
                //
                Marshal.WriteInt32(bytesWrittenPtr, count);
            }
        }

        /// <summary>
        /// Changes the seek pointer to a new location relative to the 
        /// beginning of the stream, to the end of the stream, or to the 
        /// current seek pointer.
        /// </summary>
        void IStream.Seek(long offset, int origin, IntPtr newPositionPtr)
        {
            var seekOrigin = origin switch
            {
                STREAM_SEEK_SET => SeekOrigin.Begin,
                STREAM_SEEK_CUR => SeekOrigin.Current,
                STREAM_SEEK_END => SeekOrigin.End,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            long position;
            if (this.stream.CanSeek)
            {
                position = this.stream.Seek(offset, seekOrigin);
            }
            else 
            {
                //
                // Assume we're at the right position already.
                // 
                // - If this is a NOP-seek (like Current + 0 offset),
                //   a subsequent Read or Write will succeed.
                //   
                // - If this is a Reset-seek performed at the end,
                //   we won't see any subsequent Read or Write, so
                //   we're okay too.
                //
                // This is sufficient to satisfy a client that merely
                // uses seeking to ensure the stream is set to the
                // beginning before starting to read.
                //
                this.SpeculatedPosition = position = seekOrigin switch
                {
                    SeekOrigin.Begin => offset,
                    SeekOrigin.Current => this.stream.Position + offset,
                    SeekOrigin.End => this.stream.Length + offset,
                    _ => throw new ArgumentOutOfRangeException(nameof(origin))
                };
            }

            if (newPositionPtr != IntPtr.Zero)
            {
                Marshal.WriteInt64(newPositionPtr, position);
            }
        }

        /// <summary>
        /// Changes the size of the stream object.
        /// </summary>
        void IStream.SetSize(long newSize)
        {
            this.stream.SetLength(newSize);
        }

        /// <summary>
        /// Retrieves the STATSTG structure for this stream.
        /// </summary>
        void IStream.Stat(
            out System.Runtime.InteropServices.ComTypes.STATSTG streamStats, 
            int grfStatFlag)
        {
            streamStats = new System.Runtime.InteropServices.ComTypes.STATSTG
            {
                type = STGTY_STREAM,
                cbSize = this.stream.Length,
                grfMode = 0 
            };

            if (this.stream.CanRead && this.stream.CanWrite)
            {
                streamStats.grfMode |= STGM_READWRITE;
            }
            else if (this.stream.CanRead)
            {
                streamStats.grfMode |= STGM_READ;
            }
            else if (this.stream.CanWrite)
            {
                streamStats.grfMode |= STGM_WRITE;
            }
            else
            {
                //
                // A stream that is neither readable nor writable is a closed stream.
                //
                throw new IOException("Stream is closed");
            }
        }

        /// <summary>
        /// Copies a specified number of bytes from the current seek pointer
        /// in the stream to the current seek pointer in another stream.
        /// </summary>
        void IStream.CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Discards all changes that have been made to a transacted stream 
        /// since the last Commit(Int32) call.
        /// </summary>
        void IStream.Revert()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Restricts access to a specified range of bytes in the stream.
        /// </summary>
        void IStream.LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the access restriction on a range of bytes
        /// </summary>
        void IStream.UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Ensures that any changes made to a stream object that is open in 
        /// transacted mode are reflected in the parent storage.
        /// </summary>
        void IStream.Commit(int grfCommitFlags)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a new stream object with its own seek pointer that references 
        /// the same bytes as the original stream.
        /// </summary>
        void IStream.Clone(out IStream? copy)
        {
            copy = null;
            throw new NotSupportedException();
        }

        //--------------------------------------------------------------------
        // IDisposable.
        //--------------------------------------------------------------------

        public void Dispose()
        {
            this.stream.Dispose();
        }

        //--------------------------------------------------------------------
        // P/Invoke constants.
        //--------------------------------------------------------------------

        internal const int STREAM_SEEK_SET = 0;
        internal const int STREAM_SEEK_CUR = 1;
        internal const int STREAM_SEEK_END = 2;

        internal const int STGTY_STREAM = 2;

        internal const int STGM_READ = 0;
        internal const int STGM_WRITE = 1;
        internal const int STGM_READWRITE = 2;
    }
}
