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

using System;
using System.IO;

namespace Google.Solutions.Common.IO
{
    public static class StreamExtensions
    {
        internal const int DefaultBufferSize = 64 * 1024;

        /// <summary>
        /// Reads the bytes from the current stream and 
        /// writes them to another stream. 
        /// </summary>
        public static void CopyTo(
            this Stream source,
            Stream destination, 
            IProgress<int> progress,
            int bufferSize = DefaultBufferSize)
        {
            var buffer = new byte[bufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, count);
                progress.Report(count);
            }
        }
    }
}
