//
// Copyright 2021 Google LLC
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

namespace Google.Solutions.Common.Runtime
{
    /// <summary>
    /// Disposable that invokes an action on disposal.
    /// </summary>
    public sealed class Disposable : IDisposable
    {
        private readonly Action dispose;

        private Disposable(Action dispose)
        {
            this.dispose = dispose;
        }

        /// <summary>
        /// Indicates if this object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Create a disposable that invokes an action on disposal.
        /// </summary>
        /// <param name="dispose">Action to invoke</param>
        /// <returns></returns>
        public static IDisposable Create(Action dispose)
        {
            return new Disposable(dispose);
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                this.dispose();
                this.IsDisposed = true;
            }
        }
    }
}
