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

using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Google.Solutions.Testing.Apis.Integration
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
            IMethodInfo method,
            string fingerprint,
            Func<Task<T>> provisionFunc)
        {
            //
            // Check for annotation.
            //
            if (!method.TypeInfo.Type
                .CustomAttributes
                .Where(a => a.AttributeType == typeof(UsesCloudResourcesAttribute))
                .Any())
            {
                //
                // Don't fail immediately by throwing an exception as this
                // code is run during discovery. Instead, return a task
                // that throws an exception. That way, we fail the test case.
                //
                return new ResourceTask<T>(
                    string.Empty,
                    Task.FromException<T>(
                        new MissingTestAnnotationException(
                            $"Test class {method.TypeInfo.Type.Name} must be marked with " +
                            $"[{typeof(UsesCloudResourcesAttribute).Name}] attribute to " +
                            "access cloud resources")));
            }

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
