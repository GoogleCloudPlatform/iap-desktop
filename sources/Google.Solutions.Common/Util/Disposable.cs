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

using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Google.Solutions.Common
{
    public sealed class Disposable : IDisposable
    {
        private readonly Action dispose;

        private Disposable(Action dispose)
        {
            this.dispose = dispose;
        }

        public static IDisposable For(Action dispose)
        {
            return new Disposable(dispose);
        }

        public void Dispose()
        {
            this.dispose();
        }
    }

    public class DisposableContainer : IDisposable // TODO: Add tests
    {
        private readonly IList<IDisposable> disposables;

        public DisposableContainer(params IDisposable[] disposables)
        {
            this.disposables = disposables.ToList();
        }

        public void Add(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }

        public void Dispose()
        {
            foreach (var d in this.disposables)
            {
                d.Dispose();
            }
        }
    }

    public static class DisposableExtensions
    {
        public static IComponent AsComponent(this IDisposable disposable)
        {
            return new DisposableComponent(disposable);
        }

        public static void Add(this IContainer container, IDisposable disposable)
        {
            container.Add(disposable.AsComponent());
        }

        private sealed class DisposableComponent : Component
        {
            private readonly IDisposable disposable;

            public DisposableComponent(IDisposable disposable)
            {
                this.disposable = disposable.ThrowIfNull(nameof(disposable));
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
