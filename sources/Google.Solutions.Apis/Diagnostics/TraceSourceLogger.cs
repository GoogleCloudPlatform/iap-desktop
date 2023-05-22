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

using Google.Apis.Logging;
using Google.Solutions.Common.Diagnostics;
using System;
using System.Diagnostics;

namespace Google.Solutions.Apis.Diagnostics
{
    /// <summary>
    /// Adapter class to allow Google API libraries to log to a TraceSource.
    /// </summary>
    internal class TraceSourceLogger : ILogger
    {
        private readonly TraceSource traceSource;

        public TraceSourceLogger(TraceSource traceSource)
        {
            this.traceSource = traceSource;
        }

        //---------------------------------------------------------------------
        // ILogger.
        //---------------------------------------------------------------------

        bool ILogger.IsDebugEnabled => traceSource.Switch.ShouldTrace(TraceEventType.Verbose);

        void ILogger.Debug(string message, params object[] formatArgs)
        {
            this.traceSource.TraceVerbose(message, formatArgs);
        }

        void ILogger.Error(Exception exception, string message, params object[] formatArgs)
        {
            this.traceSource.TraceError(message, formatArgs);
            this.traceSource.TraceError(exception);
        }

        void ILogger.Error(string message, params object[] formatArgs)
        {
            this.traceSource.TraceError(message, formatArgs);
        }

        void ILogger.Info(string message, params object[] formatArgs)
        {
            this.traceSource.TraceInformation(message, formatArgs);
        }

        void ILogger.Warning(string message, params object[] formatArgs)
        {
            this.traceSource.TraceWarning(message, formatArgs);
        }

        ILogger ILogger.ForType(Type type)
        {
            return this;
        }

        ILogger ILogger.ForType<T>()
        {
            return this;
        }
    }
}
