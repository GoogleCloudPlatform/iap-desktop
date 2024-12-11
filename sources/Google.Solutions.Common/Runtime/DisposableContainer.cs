//
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
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.Runtime
{
    /// <summary>
    /// Container for IDisposable objects that can be disposed
    /// at once.
    /// </summary>
    public sealed class DisposableContainer : IDisposable
    {
        private readonly List<IDisposable> disposables;

        public DisposableContainer(params IDisposable[] disposables)
        {
            this.disposables = disposables
                .EnsureNotNull()
                .ToList();
        }

        /// <summary>
        /// Add an disposable that should be disposed along
        /// with the container.
        /// </summary>
        public void Add(IDisposable disposable)
        {
            if (disposable != null)
            {
                this.disposables.Add(disposable);
            }
        }

        public void Dispose()
        {
            foreach (var d in this.disposables)
            {
                d.Dispose();
            }
        }
    }
}
