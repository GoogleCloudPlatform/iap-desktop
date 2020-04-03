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
using System.Runtime.CompilerServices;

namespace Google.Solutions.IapDesktop.Application
{
    public static class TraceSources
    {
        public static readonly TraceSource IapDesktop = new TraceSource(typeof(TraceSources).Namespace);
    }

    internal static class TraceSourceExtensions
    {
        public static void TraceVerbose(this TraceSource source, string message)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, message);
            }
        }

        public static void TraceVerbose(this TraceSource source, string message, params object[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, string.Format(message, args));
            }
        }

        public static void TraceError(this TraceSource source, string message, params object[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Error, 0, string.Format(message, args));
            }
        }

        public static TraceCallScope TraceMethod(
            this TraceSource source,
            [CallerMemberName] string method = null)
        {
            return new TraceCallScope(source, method);
        }

        public class TraceCallScope : IDisposable
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
                if (source.Switch.ShouldTrace(TraceEventType.Verbose))
                {
                    source.TraceData(
                        TraceEventType.Verbose,
                        0,
                        string.Format("Enter {0}()", this.method));
                }

                return this;
            }

            public TraceCallScope WithParameters(params object[] args)
            {
                if (source.Switch.ShouldTrace(TraceEventType.Verbose))
                {
                    source.TraceData(
                        TraceEventType.Verbose,
                        0,
                        string.Format("Enter {0}({1})", this.method, string.Join(", ", args)));
                }

                return this;
            }

            public void Dispose()
            {
                if (source.Switch.ShouldTrace(TraceEventType.Verbose))
                {
                    source.TraceData(
                        TraceEventType.Verbose,
                        0,
                        string.Format("Exit {0}()", this.method));
                }
            }
        }
    }
}
