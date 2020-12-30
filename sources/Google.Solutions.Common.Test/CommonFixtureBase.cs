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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.Common.Test
{
    public abstract class CommonFixtureBase
    {
        private static readonly TestCaseTraceListener listener = new TestCaseTraceListener();

        protected virtual IEnumerable<TraceSource> Sources => new[]
        {
            CommonTraceSources.Default
        };

        [SetUp]
        public void SetUpTracing()
        {
            foreach (var trace in this.Sources)
            {
                if (!trace.Listeners.Contains(listener))
                {
                    trace.Listeners.Add(listener);
                    trace.Switch.Level = System.Diagnostics.SourceLevels.Verbose;
                }
            }

            //
            // Enable System.Net tracing.
            //
            //NetTracing.Enabled = true;
            //NetTracing.Web.Switch.Level = System.Diagnostics.SourceLevels.Information;
            //NetTracing.Web.Listeners.Add(new TestCaseTraceListener());
        }

        private class TestCaseTraceListener : ConsoleTraceListener
        {
            private static string Prefix =>
                $"[{TestContext.CurrentContext?.Test?.Name} {DateTime.Now:o}] ";

            public override void WriteLine(object o)
            {
                base.WriteLine(o);
            }

            public override void WriteLine(object o, string category)
            {
                base.WriteLine(o, category);
            }

            public override void WriteLine(string message)
            {
                base.WriteLine(Prefix + message);
            }

            public override void WriteLine(string message, string category)
            {
                base.WriteLine(Prefix + message, category);
            }
        }
    }
}
