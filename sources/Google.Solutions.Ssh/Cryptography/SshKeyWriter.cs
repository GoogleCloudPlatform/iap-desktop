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
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.Ssh.Cryptography
{
    public sealed class SshKeyWriter : IDisposable
    {
        private readonly MemoryStream buffer = new MemoryStream();

        private void Write(int i)
        {
            var bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            this.buffer.Write(bytes, 0, 4);
        }

        public void Write(byte[] bytes)
        {
            Write(bytes.Length);
            this.buffer.Write(bytes, 0, bytes.Length);
        }

        public void Write(string s)
        {
            Write(Encoding.ASCII.GetBytes(s));
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
