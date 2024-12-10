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
    /// <summary>
    /// Base class that uses reference counting to determine
    /// when a resource should be disposed. Thread-safe.
    /// </summary>
    public abstract class ReferenceCountedDisposableBase : IDisposable
    {
        private int referenceCount = 1;

        public bool IsDisposed
        {
            get
            {
                return this.referenceCount == 0;
            }
        }

        /// <summary>
        /// Disposes the object. Called when the reference count
        /// has dropped to zero.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Increment the reference count.
        /// </summary>
        public uint AddReference()
        {
            var newRefCount = Interlocked.Increment(ref this.referenceCount);
            Debug.Assert(newRefCount > 1);
            return (uint)newRefCount;
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        /// <summary>
        /// Decrement the reference count. If the count drops to 0,
        /// the object is disposed.
        /// </summary>
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
