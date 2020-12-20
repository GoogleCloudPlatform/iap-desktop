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

using Google.Solutions.Common;
using Google.Solutions.Common.Test;
using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Google.Solutions.Ssh.Test
{
    public abstract class SshFixtureBase : CommonFixtureBase
    {
        protected override IEnumerable<TraceSource> Sources => new[]
        {
            CommonTraceSources.Default,
            SshTraceSources.Default,
        };

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
