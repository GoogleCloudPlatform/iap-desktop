//
// Copyright 2020 Google LLC
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
using System.Net;
using System.Reflection;

namespace Google.Solutions.Common.Test.Net
{
    /// <summary>
    /// Helper class to enable System.Net tracing programatically. This
    /// is only intended to be used in test cases.
    /// </summary>
    public static class NetTracing
    {
        private static Type LoggingType => typeof(HttpWebRequest)
            .Assembly
            .GetType("System.Net.Logging");

        public static TraceSource GetSource(string name)
            => (TraceSource)LoggingType
                .GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

        public static TraceSource Web => GetSource("Web");

        public static bool Enabled 
        {
            get => (bool) LoggingType.GetField(
                    "s_LoggingEnabled",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            set => LoggingType.GetField(
                    "s_LoggingEnabled",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, value);
        }
    }
}
