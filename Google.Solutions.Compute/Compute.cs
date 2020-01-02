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

using System.Diagnostics;

namespace Google.Solutions.Compute
{
    public static class Compute
    {
        public static readonly TraceSource Trace = new TraceSource(typeof(Compute).Namespace);

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
    }
}
