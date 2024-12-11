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

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Threading
{
    /// <summary>
    /// Utility methods for invoking callbacks on <c>ISynchronizeInvoke</c>
    /// objects.
    /// </summary>
    public static class SynchronousInvokeExtensions
    {
        /// <summary>
        /// Invoke a method and await its completion.
        /// </summary>
        public static Task<TResult> InvokeAsync<TResult>(
            this ISynchronizeInvoke invoker,
            Func<Task<TResult>> action)
        {
            if (!invoker.InvokeRequired)
            {
                //
                // We're on the right thread.
                //
                return action();
            }
            else
            {
                //
                // We're on the wrong thread.
                //
                var completionSource = new TaskCompletionSource<TResult>();

                var ar = invoker.BeginInvoke((Action)(() =>
                {
                    _ = action
                       .Invoke()
                       .ContinueWith(t =>
                       {
                           if (t.IsFaulted)
                           {
                               completionSource.SetException(t.Exception);
                           }
                           else
                           {
                               completionSource.SetResult(t.Result);
                           }
                       });
                }),
                null);

                return completionSource.Task;
            }
        }

        /// <summary>
        /// Invoke a method and await its completion.
        /// </summary>
        public static Task InvokeAsync(
            this ISynchronizeInvoke invoker,
            Func<Task> action)
        {
            async Task<object?> AdapterFunc()
            {
                await action().ConfigureAwait(false);
                return null;
            }

            return InvokeAsync(invoker, AdapterFunc);
        }
    }
}
