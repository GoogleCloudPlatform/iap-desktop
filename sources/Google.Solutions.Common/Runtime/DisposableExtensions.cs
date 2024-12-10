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

using Google.Solutions.Common.Util;
using System;
using System.ComponentModel;

namespace Google.Solutions.Common.Runtime
{
    /// <summary>
    /// Utility methods for working with IDisposables.
    /// </summary>
    public static class DisposableExtensions
    {
        /// <summary>
        /// Wrap a disposable into a component.
        /// </summary>
        public static IComponent ToComponent(this IDisposable disposable)
        {
            return new DisposableComponent(disposable);
        }

        private sealed class DisposableComponent : Component
        {
            private readonly IDisposable disposable;

            public DisposableComponent(IDisposable disposable)
            {
                this.disposable = disposable.ExpectNotNull(nameof(disposable));
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.disposable.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
