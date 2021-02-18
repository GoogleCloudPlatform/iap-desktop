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
using System.Text;

namespace Google.Solutions.Ssh
{
    internal class StreamingDecoder
    {
        private readonly Decoder decoder;
        private readonly Action<string> consumer;

        public StreamingDecoder(
            Encoding encoding,
            Action<string> consumer)
        {
            //
            // Use Decoder to maintain state between 
            // multiple calls. This is critical to support
            // Unicode sequences that are split across
            // multiple Decode() calls.
            //

            this.decoder = encoding.GetDecoder();
            this.consumer = consumer;
        }

        public void Decode(
            byte[] data,
            int offset,
            int length)
        {
            // N bytes can at most result in a string of length N.
            var buffer = new char[length];

            // Convert bytes, considering state from previous calls.
            var charsConverted = this.decoder.GetChars(
                data,
                offset,
                length,
                buffer,
                0);

            this.consumer(new string(buffer, 0, charsConverted));
        }

        public void Decode(byte[] data)
            => Decode(data, 0, data.Length);
    }
}
