//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.ActiveDirectory;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.ToolWindows.ActiveDirectory
{
    [TestFixture]
    public class TestJoinViewModel : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // IsValidNetbiosComputerName.
        //---------------------------------------------------------------------

        [Test]
        public void IsValidNetbiosComputerName()
        {
            Assert.IsTrue(JoinViewModel.IsValidNetbiosComputerName("a"));
            Assert.IsTrue(JoinViewModel.IsValidNetbiosComputerName("A000"));
            Assert.IsTrue(JoinViewModel.IsValidNetbiosComputerName("a-1"));

            Assert.IsFalse(JoinViewModel.IsValidNetbiosComputerName(""));
            Assert.IsFalse(JoinViewModel.IsValidNetbiosComputerName("a!"));
            Assert.IsFalse(JoinViewModel.IsValidNetbiosComputerName("a_1"));
            Assert.IsFalse(JoinViewModel.IsValidNetbiosComputerName("verylongcomputername"));
        }

        //---------------------------------------------------------------------
        // IsDomainNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void IsDomainNameInvalid_WhenDomainNameLacksDots()
        {
            var vm = new JoinViewModel();

            Assert.IsFalse(vm.IsDomainNameInvalid.Value);

            vm.DomainName.Value = "foo";

            Assert.IsTrue(vm.IsDomainNameInvalid.Value);
        }

        //---------------------------------------------------------------------
        // IsComputerNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void IsComputerNameInvalid_WhenComputerNameTooLong()
        {
            var vm = new JoinViewModel();

            Assert.IsFalse(vm.IsComputerNameInvalid.Value);

            vm.ComputerName.Value = "longcomputername";

            Assert.IsTrue(vm.IsComputerNameInvalid.Value);
        }

        [Test]
        public void IsComputerNameInvalid_WhenComputerNameNotAlphanumeric()
        {
            var vm = new JoinViewModel();

            Assert.IsFalse(vm.IsComputerNameInvalid.Value);

            vm.ComputerName.Value = "comp!name";

            Assert.IsTrue(vm.IsComputerNameInvalid.Value);
        }
    }
}
