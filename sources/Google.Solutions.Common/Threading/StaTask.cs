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

namespace Google.Solutions.Common.Threading
{
    public static class StaTask
    {
        /// <summary>
        /// Run a function on a STA background thread.
        /// </summary>
        public static Task<T> RunAsync<T>(Func<T> func)
        {
            //
            // NB. We can't use the standard thread pool as it doesn't
            // initialize a STA. Therefore, create a one-off thread.
            //
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        /// <summary>
        /// Run an action on a STA background thread.
        /// </summary>
        public static Task RunAsync(Action action)
        {
            return RunAsync<object>(() =>
            {
                action();
                return null;
            });
        }
    }
}
