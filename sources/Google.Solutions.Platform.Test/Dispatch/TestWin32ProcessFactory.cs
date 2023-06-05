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

using Google.Solutions.Platform.Dispatch;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Net;

namespace Google.Solutions.Platform.Test.Dispatch
{
    [TestFixture]
    public class TestWin32ProcessFactory
    {
        private static readonly string CmdExe
            = $"{Environment.GetFolderPath(Environment.SpecialFolder.System)}\\cmd.exe";

        //---------------------------------------------------------------------
        // CreateProcess.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExecutableNotFound_ThenCreateProcessThrowsException()
        {
            var factory = new Win32ProcessFactory();

            Assert.Throws<DispatchException>(() => factory.CreateProcess("doesnotexist.exe", null));
        }

        [Test]
        public void WhenExecutablePathFound_ThenCreateProcessSucceeds()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcess(
                CmdExe, 
                null))
            {
                Assert.IsNotNull(process.Handle);
                Assert.IsFalse(process.Handle.IsInvalid);

                process.Terminate(1);
            }
        }

        [Test]
        public void WhenJobProvided_ThenCreateProcessAddsToJob()
        {
            var job = new Mock<IWin32Job>();
            var factory = new Win32ProcessFactory(job.Object);

            using (var process = factory.CreateProcess(
                CmdExe,
                null))
            {
                job.Verify(j => j.Add(process), Times.Once);

                process.Terminate(1);
            }
        }

        //---------------------------------------------------------------------
        // CreateProcessAsUser.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUsingInvalidDomainCredentialsForNetonlyLogon_ThenCreateProcessAsUserSucceeds()
        {
            var factory = new Win32ProcessFactory();

            using (var process = factory.CreateProcessAsUser(
                CmdExe,
                null,
                LogonFlags.NetCredentialsOnly,
                new NetworkCredential("invalid", "invalid", "invalid")))
            {
                Assert.IsNotNull(process.Handle);
                Assert.IsFalse(process.Handle.IsInvalid);

                process.Terminate(1);
            }
        }

        [Test]
        public void WhenUsingInvalidLocalCredentialsForNetonlyLogon_ThenCreateProcessAsUserThrowsException()
        {
            var factory = new Win32ProcessFactory();

            Assert.Throws<DispatchException>(
                () => factory.CreateProcessAsUser(
                CmdExe,
                null,
                LogonFlags.NetCredentialsOnly,
                new NetworkCredential("invalid", "invalid")));
        }

        [Test]
        public void WhenJobProvided_ThenCreateProcessAsUserAddsToJob()
        {
            var job = new Mock<IWin32Job>();
            var factory = new Win32ProcessFactory(job.Object);

            using (var process = factory.CreateProcessAsUser(
                CmdExe,
                null,
                LogonFlags.NetCredentialsOnly,
                new NetworkCredential("invalid", "invalid", "invalid")))
            {
                job.Verify(j => j.Add(process), Times.Once);

                process.Terminate(1);
            }
        }
    }
}
