//
// Copyright 2019 Google LLC
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Google.Solutions.Common.Test.Integration
{
    public class ResourceTask<T>
    {
        //
        // Cache for existing resources so that we do not
        // unnecessarily provision the same resources
        // multiple times.
        //
        private static readonly IDictionary<string, ResourceTask<T>> cache
            = new Dictionary<string, ResourceTask<T>>();

        private readonly string fingerprint;
        private readonly Task<T> task;

        private ResourceTask(
            string fingerprint,
            Task<T> task)
        {
            this.fingerprint = fingerprint;
            this.task = task;
        }

        public override string ToString()
            => this.fingerprint;

        public TaskAwaiter<T> GetAwaiter()
            => this.task.GetAwaiter();

        public static ResourceTask<T> ProvisionOnce(
            string fingerprint,
            Func<Task<T>> provisionFunc)
        {
            lock (cache)
            {
                if (cache.TryGetValue(fingerprint, out ResourceTask<T> cached))
                {
                    return cached;
                }
                else
                {
                    var task = new ResourceTask<T>(fingerprint, provisionFunc());
                    cache.Add(fingerprint, task);
                    return task;
                }
            }
        }
    }
}
