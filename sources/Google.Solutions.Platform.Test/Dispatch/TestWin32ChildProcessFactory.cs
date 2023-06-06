﻿//
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

using Google.Solutions.Platform.Dispatch;
using NUnit.Framework;
using System;
using System.Net;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32ChildProcessFactory
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // Contains.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProcessFound_ThenContainsReturnsTrue()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            using (var process = factory.CreateProcess(CmdExe, null))
            {
                Assert.IsTrue(factory.Contains(process));
                Assert.IsTrue(factory.Contains(process.Id));
            }
        }

        [Test]
        public void WhenProcessNotFound_ThenContainsReturnsFalse()
        {
            using (var factory = new Win32ChildProcessFactory(true))
            {
                Assert.IsFalse(factory.Contains(4));
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTerminateOnCloseIsFalse_ThenDisposeKeepsProcesses()
        {
            var factory = new Win32ChildProcessFactory(false);
            var process = factory.CreateProcess(CmdExe, null);
            process.Resume();

            Assert.IsTrue(process.IsRunning);
            factory.Dispose();

            Assert.IsTrue(process.IsRunning);
            process.Terminate(0);
            process.Dispose();
        }

        [Test]
        public void WhenTerminateOnCloseIsTrue_ThenDisposeTerminatesProcesses()
        {
            var factory = new Win32ChildProcessFactory(true);
            var process = factory.CreateProcess(CmdExe, null);
            process.Resume();

            Assert.IsTrue(process.IsRunning);
            factory.Dispose();

            Assert.IsFalse(process.IsRunning);
            process.Dispose();
        }
    }
}
