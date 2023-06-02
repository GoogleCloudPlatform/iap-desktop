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

using Google.Solutions.Platform.Scheduling;
using NUnit.Framework;
using System;
using System.ComponentModel;

namespace Google.Solutions.Platform.Test.Scheduling
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

            Assert.Throws<Win32Exception>(() => factory.CreateProcess("doesnotexist.exe", null));
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
    }
}
