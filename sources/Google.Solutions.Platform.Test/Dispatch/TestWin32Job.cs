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
using System.Linq;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32Job
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // KillOnJobClose.
        //---------------------------------------------------------------------

        [Test]
        public void WhenKillOnJobCloseIsTrue_ThenDisposeKillsProcesses()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var job = new Win32Job(true);

                job.Add(process);
                process.Resume();
                Assert.IsTrue(process.IsRunning);

                job.Dispose();

                Assert.IsFalse(process.IsRunning);
            }
        }

        [Test]
        public void WhenKillOnJobCloseIsFalse_ThenDisposeKeepsProcesses()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(CmdExe, null))
            {
                var job = new Win32Job(false);

                job.Add(process);
                process.Resume();
                Assert.IsTrue(process.IsRunning);

                job.Dispose();

                Assert.IsTrue(process.IsRunning);
                process.Terminate(0);
            }
        }

        //---------------------------------------------------------------------
        // Add, Contains.
        //---------------------------------------------------------------------

        [Test]
        public void WhenAdded_ThenContainsReturnsTrue()
        {
            var factory = new Win32ProcessFactory();

            using (var job = new Win32Job(true))
            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                Assert.IsFalse(job.Contains(process));
                Assert.IsFalse(job.Contains(process.Id));

                job.Add(process);
                job.Add(process); // Again.

                Assert.IsTrue(job.Contains(process));
                Assert.IsTrue(job.Contains(process.Id));
            }
        }

        [Test]
        public void WhenProcessDoesNotExist_ThenContainsThrowsException()
        {
            using (var job = new Win32Job(true))
            {
                Assert.Throws<DispatchException>(
                    () => job.Contains(uint.MaxValue));
            }
        }

        //---------------------------------------------------------------------
        // ProcessIds.
        //---------------------------------------------------------------------

        [Test]
        public void WhenJobEmpty_ThenProcessIdsIsEmpty()
        {
            using (var job = new Win32Job(true))
            {
                CollectionAssert.IsEmpty(job.ProcessIds);
            }
        }

        [Test]
        public void WhenJobContainsProcesses_ThenProcessIdsReturnsId()
        {
            var factory = new Win32ProcessFactory();

            using (var job = new Win32Job(true))
            using (var process1 = factory.CreateProcess(CmdExe, null))
            using (var process2 = factory.CreateProcess(CmdExe, null))
            {
                job.Add(process1);
                job.Add(process2);

                var ids = job.ProcessIds;
                Assert.AreEqual(2, ids.Count());
                CollectionAssert.Contains(ids, process1.Id);
                CollectionAssert.Contains(ids, process2.Id);
            }
        }
    }
}
