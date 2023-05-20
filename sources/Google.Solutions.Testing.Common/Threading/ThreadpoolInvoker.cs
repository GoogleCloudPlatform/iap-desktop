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
// Profileific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Threading;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Common.Threading
{
    /// <summary>
    /// An ISynchronizeInvoke implementation that performs invocation
    /// on the thread pool.
    /// </summary>
    public class ThreadpoolInvoker : ISynchronizeInvoke
    {
        private readonly object outstandingLock = new object();
        private int outstanding = 0;
        private readonly ManualResetEvent idle = new ManualResetEvent(false);

        public Task AwaitPendingInvocations()
        {
            return this.idle.Await();
        }

        //---------------------------------------------------------------------
        // ISynchronizeInvoke.
        //---------------------------------------------------------------------

        public bool InvokeRequired => true;

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            lock (this.outstandingLock)
            {
                this.outstanding++;
                this.idle.Reset();
            }

            return new Result()
            {
                Task = Task.Run(() =>
                {
                    try
                    {
                        return method.DynamicInvoke(args);
                    }
                    finally
                    {
                        lock (this.outstandingLock)
                        {
                            this.outstanding--;
                            if (this.outstanding == 0)
                            {
                                this.idle.Set();
                            }
                        }
                    }
                })
            };
        }

        public object EndInvoke(IAsyncResult result)
        {
            return ((Result)result).Task.Result;
        }

        public object Invoke(Delegate method, object[] args)
        {
            return method.DynamicInvoke(args);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Result : IAsyncResult
        {
            public bool IsCompleted => true;

            public WaitHandle AsyncWaitHandle => null;

            public object AsyncState => null;

            public bool CompletedSynchronously => true;

            internal Task<object> Task { get; set; }
        }
    }
}
