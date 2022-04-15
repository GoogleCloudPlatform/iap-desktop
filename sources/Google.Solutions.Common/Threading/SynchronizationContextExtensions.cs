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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Threading
{
    public static class SynchronizationContextExtensions
    {
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

        public static Task RunAsync(
            this SynchronizationContext context,
            Action func)
        {
            return RunAsync<object>(
                context,
                () =>
                {
                    func();
                    return null;
                });
        }
    }
}
