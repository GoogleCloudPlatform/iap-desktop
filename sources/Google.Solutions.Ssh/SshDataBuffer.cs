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

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// Buffer for writing SSH-structured data,
    /// see RFC4251 section 5.
    /// </summary>
    internal sealed class SshDataBuffer : IDisposable
    {
        private readonly MemoryStream buffer = new MemoryStream();

        private void WriteRaw(int i)
        {
            var bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            this.buffer.Write(bytes, 0, 4);
        }

        private void WriteRaw(byte b)
        {
            this.buffer.Write(new byte[] { b }, 0, 1);
        }

        public void WriteBytes(byte[] bytes)
        {
            WriteRaw(bytes.Length);
            this.buffer.Write(bytes, 0, bytes.Length);
        }

        public void WriteString(string s)
        {
            WriteBytes(Encoding.ASCII.GetBytes(s));
        }

        public void WriteMpint(byte[] bigEndian)
        {
            if (bigEndian.All(b => b == 0))
            {
                WriteRaw(0);
            }
            else
            {
                if ((bigEndian[0] & 0x80) == 128)
                {
                    //
                    // If the most significant bit would be set for
                    // a positive number, the number MUST be preceded
                    // by a zero byte.
                    //
                    WriteRaw(bigEndian.Length + 1);
                    WriteRaw((byte)0);
                }
                else
                {
                    WriteRaw(bigEndian.Length);
                }

                this.buffer.Write(bigEndian, 0, bigEndian.Length);
            }
        }

        public byte[] ToArray()
        {
            this.buffer.Flush();
            return this.buffer.ToArray();
        }

        public void Dispose()
        {
            this.buffer.Dispose();
        }
    }
}
