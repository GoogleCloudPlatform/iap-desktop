﻿//
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

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Interop
{
    public static class SafeHandleExtensions
    {
        /// <summary>
        /// Create a wait handle for a handle so that you can 
        /// use WaitHandle.WaitXxx.
        /// </summary>
        public static WaitHandle ToWaitHandle(
            this SafeHandle handle,
            bool transferOwnership)
        {
            return new WaitHandleWrapper()
            {
                SafeWaitHandle = new SafeWaitHandle(
                    handle.DangerousGetHandle(),
                    transferOwnership)
            };
        }

        /// <summary>
        /// Wait for handle to be signalled.
        /// </summary>
        /// <returns>true if signalled, false if timeout elapsed</returns>
        public static Task<bool> WaitAsync(
            this WaitHandle waitHandle,
            TimeSpan timeout)
        {
            var completionSource = new TaskCompletionSource<bool>();

            var registration = ThreadPool.RegisterWaitForSingleObject(
                waitHandle,
                (_, timeoutElapsed) =>
                {
                    //
                    // Return true if the process was signalled (= exited)
                    // within the timeout, or false otherwise.
                    //
                    completionSource.SetResult(!timeoutElapsed);
                },
                null,
                (uint)timeout.TotalMilliseconds,
                true);

            return completionSource.Task
                .ContinueWith(t =>
                {
                    registration.Unregister(waitHandle);
                    return t.Result;
                });
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class WaitHandleWrapper : WaitHandle
        {
        }
    }
}
