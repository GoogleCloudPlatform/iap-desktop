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

using Google.Solutions.IapDesktop.Application.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Application.Mocks
{
    public class SynchronousJobService : IJobService
    {
        public int JobsCompleted { get; private set; } = 0;

        public Task<T> RunAsync<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc)
        {
            var result = jobFunc(CancellationToken.None);
            this.JobsCompleted++;
            return result;
        }

        public Task RunAsync(JobDescription jobDescription,
            Func<CancellationToken, Task> jobFunc)
        {
            jobFunc(CancellationToken.None);
            this.JobsCompleted++;
            return Task.CompletedTask;
        }
    }
}
