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

using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Threading
{

    public static class WaitHandleExtensions
    {
        public static Task<bool> WaitAsync(this WaitHandle waitHandle)//TODO: delete
        {
            var tcs = new TaskCompletionSource<object?>();

            var registration = ThreadPool.RegisterWaitForSingleObject(
                waitHandle,
                (_, __) => { tcs.SetResult(null); },
                null,
                -1,
                true);

            return tcs.Task.ContinueWith(t =>
            {
                try
                {
                    registration.Unregister(waitHandle);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
