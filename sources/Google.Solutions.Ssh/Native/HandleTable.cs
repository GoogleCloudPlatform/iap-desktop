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

using Google.Solutions.Common.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Track open safe handles. Only active in Debug builds, for diagnostics only.
    /// </summary>
    [SkipCodeCoverageAttribute("For debug purposes only")]
    internal static class HandleTable
    {
        private static readonly object handlesLock;
        private static readonly IDictionary<SafeHandle, string> handles;

        static HandleTable()
        {
#if DEBUG
            handlesLock = new object();
            handles = new Dictionary<SafeHandle, string>();
#endif
        }

        [Conditional("DEBUG")]
        public static void Clear()
        {
            lock (handlesLock)
            {
                handles.Clear();
            }
        }

        [Conditional("DEBUG")]
        public static void OnHandleCreated(SafeHandle handle, string description)
        {
            if (handle.IsInvalid)
            {
                return;
            }

            lock (handlesLock)
            {
                Debug.Assert(!handles.ContainsKey(handle));
                handles.Add(handle, description);
            }
        }

        [Conditional("DEBUG")]
        public static void OnHandleClosed(SafeHandle handle)
        {
            lock (handlesLock)
            {
                handles.Remove(handle);
            }
        }

        [Conditional("DEBUG")]
        public static void DumpOpenHandles()
        {
            Debug.Assert(handles.Count == 0);
            foreach (var entry in handles)
            {
                Debug.WriteLine("Leaked handle {0}: {1}", entry.Key, entry.Value);
            }
        }

        public static int HandleCount => handles != null
            ? handles.Count
            : 0;
    }
}
