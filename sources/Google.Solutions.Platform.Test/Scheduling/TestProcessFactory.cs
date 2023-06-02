//
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

using Google.Solutions.Platform.Net;
using Google.Solutions.Platform.Scheduling;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading;

namespace Google.Solutions.Platform.Test.Scheduling
{
    [TestFixture]
    public class TestProcessFactory
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";
        private static readonly string NotepadExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\notepad.exe";

        //---------------------------------------------------------------------
        // CreateProcess.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExecutableNotFound_ThenCreateProcessThrowsException()
        {
            var factory = new ProcessFactory();

            Assert.Throws<Win32Exception>(() => factory.CreateProcess("doesnotexist.exe", null));
        }

        [Test]
        public void WhenExecutablePathFound_ThenCreateProcessSucceeds()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe, 
                null))
            {
                Assert.IsNotNull(process.Handle);
                Assert.IsFalse(process.Handle.IsInvalid);

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // ImageName.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExecutableContainsPath_ThenImageNameReturnsFilename()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                Assert.AreEqual("cmd.exe", process.ImageName);

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // Resume.
        //---------------------------------------------------------------------

        [Test]
        public void WhenResumedAlready_ThenResumeSucceeds()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                Assert.IsNotNull(process.Handle);
                Assert.IsFalse(process.Handle.IsInvalid);

                process.Resume();
                process.Resume(); // Again.

                Assert.IsTrue(process.IsRunning);

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // Terminate.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTerminatedAlready_ThenTerminateThrowsException()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                process.Resume();

                Assert.IsNotNull(process.Handle);
                Assert.IsFalse(process.Handle.IsInvalid);
                Assert.IsTrue(process.IsRunning);

                process.Terminate(1);

                Assert.IsFalse(process.IsRunning);

                Assert.Throws<Win32Exception>(() => process.Terminate(1));
            }
        }

        //---------------------------------------------------------------------
        // Close.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProcessHasNoWindows_ThenCloseReturnsFalse()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                process.Resume();
                Assert.AreEqual(0, process.WindowCount);

                Assert.IsFalse(process.Close());

                process.Terminate(1);
            }
        }

        [Test]
        public void WhenProcessHasWindows_ThenCloseReturnsTrue()
        {
            var factory = new ProcessFactory();

            using (var process = factory.CreateProcess(
                NotepadExe,
                null))
            {
                process.Resume();

                //
                // Give notepad some time to create its window.
                //
                for (int i = 0; i < 100 && process.WindowCount == 0; i++)
                {
                    Thread.Sleep(10);
                }

                Assert.IsTrue(process.Close());
                
                Thread.Sleep(100);

                Assert.IsFalse(process.IsRunning);
            }
        }
    }
}
