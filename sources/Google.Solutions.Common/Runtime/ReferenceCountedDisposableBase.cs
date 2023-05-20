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

using System;
using System.Diagnostics;
using System.Threading;

namespace Google.Solutions.Common.Runtime
{
    public abstract class ReferenceCountedDisposableBase : IDisposable
    {
        private int referenceCount = 1;

        public bool IsDisposed => this.referenceCount == 0;

        protected virtual void Dispose(bool disposing)
        {
        }

        public uint AddReference()
        {
            var newRefCount = Interlocked.Increment(ref this.referenceCount);
            Debug.Assert(newRefCount > 1);
            return (uint)newRefCount;
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            var newRefCount = Interlocked.Decrement(ref this.referenceCount);
            if (newRefCount == 0)
            {
                Dispose(true);
                Debug.Assert(this.IsDisposed, "base.Dispose was not called");
                GC.SuppressFinalize(this);
            }
            else if (newRefCount < 0)
            {
                throw new ObjectDisposedException("Object was disposed");
            }
        }

        ~ReferenceCountedDisposableBase()
        {
            Debug.Assert(
                this.IsDisposed,
                "Object was not disposed or base.Dispose was not called");
            Dispose(disposing: false);
        }
    }
}
