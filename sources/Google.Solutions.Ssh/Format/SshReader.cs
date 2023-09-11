//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Format;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Google.Solutions.Ssh.Format
{
    /// <summary>
    /// Reader for SSH-structured data, see RFC4251 section 5.
    /// </summary>
    internal class SshReader : IDisposable
    {
        private readonly Stream stream;

        public SshReader(Stream stream)
        {
            Debug.Assert(stream.CanRead);
            this.stream = stream.ExpectNotNull(nameof(stream));
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public byte ReadByte()
        {
            //
            // A byte represents an arbitrary 8 - bit value(octet).Fixed length
            // data is sometimes represented as an array of bytes, written
            // byte[n], where n is the number of bytes in the array.
            //
            var buffer = new byte[1];
            if (this.stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                return buffer[0];
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public bool ReadBoolean()
        {
            //
            // A boolean value is stored as a single byte.The value 0
            // represents FALSE, and the value 1 represents TRUE.  All non-zero
            // values MUST be interpreted as TRUE; however, applications MUST NOT
            // store values other than 0 and 1.
            //
            return ReadByte() == 1;
        }

        public uint ReadUint32()
        {
            //
            // Represents a 32 - bit unsigned integer.  Stored as four bytes in the
            // order of decreasing significance (network byte order).For
            // example: the value 699921578 (0x29b7f4aa) is stored as 29 b7 f4
            // aa.
            //
            var buffer = new byte[4];
            if (this.stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                return BigEndian.DecodeUInt32(buffer, 0);
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public ulong ReadUint64()
        {
            //
            // Represents a 64 - bit unsigned integer.  Stored as eight bytes in
            // the order of decreasing significance (network byte order).
            //
            var buffer = new byte[8];
            if (this.stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                return BigEndian.DecodeUInt64(buffer, 0);
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public byte[] ReadByteString()
        {
            var length = ReadUint32();
            var buffer = new byte[length];
            if (this.stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                return buffer;
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public string ReadString()
        {
            //
            // Arbitrary length binary string.Strings are allowed to contain
            // arbitrary binary data, including null characters and 8 - bit
            // characters.They are stored as a uint32 containing its length
            // (number of bytes that follow) and zero (= empty string) or more
            // bytes that are the value of the string.Terminating null
            // characters are not used.
            // 
            // Strings are also used to store text.In that case, US-ASCII is
            // used for internal names, and ISO-10646 UTF-8 for text that might
            // be displayed to the user.The terminating null character SHOULD
            // 
            // NOT normally be stored in the string.  For example: the US-ASCII
            // 
            // string "testing" is represented as 00 00 00 07 t e s t i n g. The
            // UTF-8 mapping does not alter the encoding of US-ASCII characters.
            //
            return Encoding.ASCII.GetString(ReadByteString());
        }

        public IEnumerable<byte> ReadMultiPrecisionInteger()
        {
            // 
            // Represents multiple precision integers in two's complement format,
            // stored as a string, 8 bits per byte, MSB first.  Negative numbers
            // have the value 1 as the most significant bit of the first byte of
            // the data partition.  If the most significant bit would be set for
            // a positive number, the number MUST be preceded by a zero byte.
            // Unnecessary leading bytes with the value 0 or 255 MUST NOT be
            // included.  The value zero MUST be stored as a string with zero
            // bytes of data.
            // 
            var length = ReadUint32();
            if (length == 0)
            {
                return new byte[] { 0 };
            }
            else
            {
                var buffer = new byte[length];
                if (this.stream.Read(buffer, 0, buffer.Length) == buffer.Length)
                {
                    if (buffer[0] == 0)
                    {
                        return buffer.Skip(1);
                    }
                    else
                    {
                        return buffer;
                    }
                }
                else
                {
                    throw new EndOfStreamException();
                }
            }
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.stream.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
