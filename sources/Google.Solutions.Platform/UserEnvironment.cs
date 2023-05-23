//
// Copyright 2023 Google LLC
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

using System.Runtime.InteropServices;
using System.Text;

namespace Google.Solutions.Platform
{
    public class UserEnvironment
    {
        /// <summary>
        /// Expands environment-variable strings and replaces them with 
        /// the values defined for the current user.
        /// </summary>
        public static string ExpandEnvironmentStrings(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            var buffer = new StringBuilder(0);
            var sizeRequired = NativeMethods.ExpandEnvironmentStrings(
                source,
                buffer,
                buffer.Capacity);

            buffer.EnsureCapacity(sizeRequired);
            NativeMethods.ExpandEnvironmentStrings(
                source,
                buffer,
                buffer.Capacity);

            return buffer.ToString();
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int ExpandEnvironmentStrings(
                [MarshalAs(UnmanagedType.LPTStr)] string source,
                [Out] StringBuilder destination,
                int size);
        }
    }
}
