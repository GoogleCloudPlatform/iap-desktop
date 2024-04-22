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
using System.Diagnostics;
using System.Runtime.InteropServices;

#if DEBUG
using System.Collections.Generic;
#endif

#pragma warning disable CA1810 // Initialize reference type static fields inline

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Track open safe handles. Only active in Debug builds, for diagnostics only.
    /// </summary>
    [SkipCodeCoverage("For debug purposes only")]
    internal static class HandleTable
    {
#if DEBUG
        private static readonly object handlesLock = new object();
        private static readonly Dictionary<SafeHandle, string> handles = new Dictionary<SafeHandle, string>();

#endif

        [Conditional("DEBUG")]
        public static void Clear()
        {
#if DEBUG
            lock (handlesLock)
            {
                handles.Clear();
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void OnHandleCreated(SafeHandle handle, string description)
        {
#if DEBUG
            if (handle.IsInvalid)
            {
                return;
            }

            lock (handlesLock)
            {
                Debug.Assert(!handles.ContainsKey(handle));
                handles.Add(handle, description);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void OnHandleClosed(SafeHandle handle)
        {
#if DEBUG
            lock (handlesLock)
            {
                handles.Remove(handle);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void DumpOpenHandles()
        {
#if DEBUG
            Debug.Assert(handles.Count == 0);
            foreach (var entry in handles)
            {
                Debug.WriteLine("Leaked handle {0}: {1}", entry.Key, entry.Value);
            }
#endif
        }

        public static int HandleCount
        {
#if DEBUG
            get => handles != null
                ? handles.Count
                : 0;
#else
            get => 0;
#endif
        }
    }
}
