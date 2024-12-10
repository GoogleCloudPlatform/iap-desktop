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
using System.Diagnostics;

namespace Google.Solutions.Common.Diagnostics
{
    /// <summary>
    /// Trace entry and exit of a scope.
    /// </summary>
    public sealed class TraceCallScope : IDisposable
    {
        private readonly TraceSource source;
        private readonly string method;

        public TraceCallScope(TraceSource source, string method)
        {
            this.source = source;
            this.method = method;
        }

        public TraceCallScope WithoutParameters()
        {
            if (this.source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                this.source.TraceData(
                    TraceEventType.Verbose,
                    0,
                    string.Format("Enter {0}()", this.method));
            }

            return this;
        }

        public TraceCallScope WithParameters(params object?[] args)
        {
            if (this.source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                this.source.TraceData(
                    TraceEventType.Verbose,
                    0,
                    string.Format("Enter {0}({1})", this.method, string.Join(", ", args)));
            }

            return this;
        }

        public void Dispose()
        {
            if (this.source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                this.source.TraceData(
                    TraceEventType.Verbose,
                    0,
                    string.Format("Exit {0}()", this.method));
            }
        }
    }
}
