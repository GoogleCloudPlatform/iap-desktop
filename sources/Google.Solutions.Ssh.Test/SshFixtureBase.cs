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

using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Google.Solutions.Ssh.Test
{
    public abstract class SshFixtureBase
    {
        private static readonly ConsoleTraceListener listener = new ConsoleTraceListener();

        private static readonly TraceSource[] Traces = new[]
        {
            Google.Solutions.Common.TraceSources.Common,
            SshTraceSources.Default,
        };

        //---------------------------------------------------------------------
        // Tracing.
        //---------------------------------------------------------------------

        [SetUp]
        public void SetUpTracing()
        {
            foreach (var trace in Traces)
            {
                if (!trace.Listeners.Contains(listener))
                {
                    listener.TraceOutputOptions = TraceOptions.DateTime;
                    trace.Listeners.Add(listener);
                    trace.Switch.Level = System.Diagnostics.SourceLevels.Verbose;
                }
            }

            listener.WriteLine("Start " + TestContext.CurrentContext.Test.FullName);
        }

        [TearDown]
        public void TearDownTracing()
        {
            listener.WriteLine("End " + TestContext.CurrentContext.Test.FullName);
        }

        //---------------------------------------------------------------------
        // Handle tracking.
        //---------------------------------------------------------------------

        [SetUp]
        public void ClearOpenHandles()
        {
            HandleTable.Clear();
        }

        [TearDown]
        public void CheckOpenHandles()
        {
            HandleTable.DumpOpenHandles();
            Assert.AreEqual(0, HandleTable.HandleCount);
        }

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected static SshSession CreateSession()
        {
            var session = new SshSession();
            session.SetTraceHandler(
                LIBSSH2_TRACE.SOCKET | LIBSSH2_TRACE.ERROR | LIBSSH2_TRACE.CONN |
                                       LIBSSH2_TRACE.AUTH | LIBSSH2_TRACE.KEX,
                Console.WriteLine);

            session.Timeout = TimeSpan.FromSeconds(5);
            return session;
        }

    }
}
