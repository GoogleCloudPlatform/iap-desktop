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
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("a"), Is.True);
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("A000"), Is.True);
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("a-1"), Is.True);

            Assert.That(JoinViewModel.IsValidNetbiosComputerName(""), Is.False);
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("a!"), Is.False);
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("a_1"), Is.False);
            Assert.That(JoinViewModel.IsValidNetbiosComputerName("verylongcomputername"), Is.False);
        }

        //---------------------------------------------------------------------
        // IsDomainNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void IsDomainNameInvalid_WhenDomainNameLacksDots()
        {
            var vm = new JoinViewModel();

            Assert.That(vm.IsDomainNameInvalid.Value, Is.False);

            vm.DomainName.Value = "foo";

            Assert.That(vm.IsDomainNameInvalid.Value, Is.True);
        }

        //---------------------------------------------------------------------
        // IsComputerNameInvalid.
        //---------------------------------------------------------------------

        [Test]
        public void IsComputerNameInvalid_WhenComputerNameTooLong()
        {
            var vm = new JoinViewModel();

            Assert.That(vm.IsComputerNameInvalid.Value, Is.False);

            vm.ComputerName.Value = "longcomputername";

            Assert.That(vm.IsComputerNameInvalid.Value, Is.True);
        }

        [Test]
        public void IsComputerNameInvalid_WhenComputerNameNotAlphanumeric()
        {
            var vm = new JoinViewModel();

            Assert.That(vm.IsComputerNameInvalid.Value, Is.False);

            vm.ComputerName.Value = "comp!name";

            Assert.That(vm.IsComputerNameInvalid.Value, Is.True);
        }
    }
}
