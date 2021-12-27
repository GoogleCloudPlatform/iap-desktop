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

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.TunnelsViewer
{
    public static class ByteSizeFormatter
    {
        public static string Format(ulong volumeInBytes)
        {
            string suffix;
            double readable;
            if (volumeInBytes >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (volumeInBytes >> 50);
            }
            else if (volumeInBytes >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (volumeInBytes >> 40);
            }
            else if (volumeInBytes >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (volumeInBytes >> 30);
            }
            else if (volumeInBytes >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (volumeInBytes >> 20);
            }
            else if (volumeInBytes >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (volumeInBytes >> 10);
            }
            else if (volumeInBytes >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = volumeInBytes;
            }
            else
            {
                return volumeInBytes.ToString("0 B"); // Byte
            }

            readable /= 1024;
            return readable.ToString("0.# ") + suffix;
        }
    }
}
