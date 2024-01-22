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

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs

namespace Google.Solutions.Common.Threading
{
    /// <summary>
    /// Utility extension methods for SynchronizationContext.
    /// </summary>
    public static class SynchronizationContextExtensions
    {
        /// <summary>
        /// Posts a callback and returns a Task to await its completion.
        /// </summary>
        public static Task<T> RunAsync<T>(
            this SynchronizationContext context,
            Func<T> func)
        {
            //
            // Force continuations to run on their execution
            // context, not ours.
            //
            var completionSource = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            //
            // Execute the callback in the context, and signal the
            // task when it's done.
            //
            context.Post(_ =>
            {
                try
                {
                    completionSource.SetResult(func());
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                }
            },
            null);

            return completionSource.Task;
        }

        /// <summary>
        /// Posts a callback and returns a Task to await its completion.
        /// </summary>
        public static Task RunAsync(
            this SynchronizationContext context,
            Action func)
        {
            return RunAsync<object?>(
                context,
                () =>
                {
                    func();
                    return null;
                });
        }

        /// <summary>
        /// Send a callback and pass its return value.
        /// </summary>
        public static T Send<T>(
            this SynchronizationContext context,
            Func<T> func)
        {
            var value = default(T);
            context.Send(_ =>
            {
                value = func();
            },
            null);

            return value!;
        }

        /// <summary>
        /// Post a callback that doesn not expect any parameters.
        /// </summary>
        public static void Post(
            this SynchronizationContext context,
            Action func)
        {
            context.Post(_ => func(), null);
        }
    }
}
