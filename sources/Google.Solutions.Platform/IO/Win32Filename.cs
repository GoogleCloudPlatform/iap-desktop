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

using System.IO;
using System.Linq;

namespace Google.Solutions.Platform.Interop
{
    /// <summary>
    /// Utility methods for dealing with Win32 filenames.
    /// </summary>
    public static class Win32Filename
    {
        private static readonly string[] DosDevices =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        internal static string StripExtension(string name)
        {
            var dotIndex = name.LastIndexOf('.');
            return dotIndex <= 0
                ? name
                : name.Substring(0, dotIndex);
        }

        /// <summary>
        /// Check if a file name is a valid Windows file name.
        /// </summary>
        public static bool IsValidFilename(string name)
        {
            //
            // Cf. https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file
            //
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }
            else if (DosDevices.Any(n => n == StripExtension(name).ToUpper()))
            {
                return false;
            }
            else if (name.EndsWith("."))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Escape a file name so that it becomes a valid Windows file name.
        /// </summary>
        public static string EscapeFilename(string name)
        {
            if (DosDevices.Any(n => n == StripExtension(name).ToUpper()))
            {
                name = "_" + name;
            }

            if (name.EndsWith("."))
            {
                name += "_";
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }
}
