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

using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Google.Solutions.Common.Diagnostics
{
    /// <summary>
    /// Utility methods for <c>TraceSource</c>.
    /// </summary>
    public static class TraceSourceExtensions
    {
        public static void TraceVerbose(this TraceSource source, string message)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, message);
            }
        }

        public static void TraceVerbose(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                source.TraceData(TraceEventType.Verbose, 0, string.Format(message, args));
            }
        }

        public static void TraceWarning(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Warning))
            {
                source.TraceData(TraceEventType.Warning, 0, string.Format(message, args));
            }
        }

        public static void TraceError(this TraceSource source, string message, params object?[] args)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Error))
            {
                source.TraceData(TraceEventType.Error, 0, string.Format(message, args));
            }
        }

        public static void TraceError(this TraceSource source, Exception exception)
        {
            if (source.Switch.ShouldTrace(TraceEventType.Error))
            {
                source.TraceData(TraceEventType.Error, 0, exception.Unwrap());
            }
        }

        public static TraceCallScope TraceMethod(
            this TraceSource source,
            [CallerMemberName] string? method = null)
        {
            return new TraceCallScope(source, method ?? "unknown");
        }
    }
}
