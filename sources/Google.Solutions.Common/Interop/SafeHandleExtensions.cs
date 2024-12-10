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

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Interop
{
    /// <summary>
    /// Utility methods for working with SafeHandles.
    /// </summary>
    public static class SafeHandleExtensions
    {
        public static readonly TimeSpan Infinite 
            = TimeSpan.FromMilliseconds(int.MaxValue);

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
        public static Task WaitAsync(
            this WaitHandle waitHandle,
            CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<object?>();

            var registration = ThreadPool.RegisterWaitForSingleObject(
                waitHandle,
                (_, __) => completionSource.SetResult(null),
                null,
                Infinite,
                true);

            cancellationToken.Register(() =>
            {
                registration.Unregister(waitHandle);
                completionSource.SetCanceled();
            });

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
