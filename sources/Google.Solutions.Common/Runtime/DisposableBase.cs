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

namespace Google.Solutions.Common.Runtime
{
    /// <summary>
    /// Base class for disposable objects.
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
#if DEBUG
        private readonly string stackTrace = Environment.StackTrace;
#endif
        public bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            this.IsDisposed = true;
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                Dispose(true);

                //
                // The override is supposed to call base.Dispose.
                //
                Debug.Assert(this.IsDisposed, "base.Dispose was not called");
            }

            GC.SuppressFinalize(this);
        }

        ~DisposableBase()
        {
#if DEBUG
            Debug.Assert(
                this.IsDisposed,
                "Object was not disposed or base.Dispose was not called\n\n" +
                "Constructor was called at:\n\n" +
                this.stackTrace);
#endif
            Dispose(disposing: false);
        }
    }
}
