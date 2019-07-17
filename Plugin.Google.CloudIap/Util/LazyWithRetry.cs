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

namespace Plugin.Google.CloudIap.Util
{
    public class LazyWithRetry<T> where T : class
    {
        private readonly Func<T> valueFactory;
        private readonly object instanceLock = new object();
        private T instance;

        public LazyWithRetry(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory;
            this.instance = null;
        }

        public T Value
        {
            get
            {
                lock (instanceLock)
                {
                    if (this.instance == null)
                    {
                        // Create new instance. If it throws an exception, we will
                        // simply retry next time. System.Lazy, in contrast, would
                        // cache and rethrow an exception in this case.
                        this.instance = this.valueFactory();
                    }

                    return this.instance;
                }
            }
        }
    }
}
