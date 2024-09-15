//
// Copyright 2024 Google LLC
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

using Google.Solutions.Settings.Collection;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Settings.Test.Collection
{
    [TestFixture]
    public class TestGroupPolicyAwareRepository
    {
        private const string KeyPath = @"Software\Google\__Test";
        private const string UserPolicyKeyPath = @"Software\Google\__TestUserPolicy";
        private const string MachinePolicyKeyPath = @"Software\Google\__TestMachinePolicy";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(KeyPath, false);
            this.hkcu.DeleteSubKeyTree(UserPolicyKeyPath, false);
            this.hkcu.DeleteSubKeyTree(MachinePolicyKeyPath, false);
        }

        protected RegistryKey CreateKey()
        {
            return this.hkcu.CreateSubKey(KeyPath);
        }

        protected RegistryKey CreateUserPolicyKey()
        {
            return this.hkcu.CreateSubKey(UserPolicyKeyPath);
        }

        protected RegistryKey CreateMachinePolicyKey()
        {
            return this.hkcu.CreateSubKey(MachinePolicyKeyPath);
        }

        private class EmptyCollection : ISettingsCollection
        {
            public IEnumerable<ISetting> Settings => Enumerable.Empty<ISetting>();
        }

        private class SampleRepository : GroupPolicyAwareRepository<EmptyCollection>
        {
            public SampleRepository(
                RegistryKey settingsKey,
                RegistryKey machinePolicyKey,
                RegistryKey userPolicyKey)
                : base(settingsKey, machinePolicyKey, userPolicyKey)
            {
            }

            internal new ISettingsStore Store => base.Store;

            protected override EmptyCollection LoadSettings(ISettingsStore store)
            {
                return new EmptyCollection();
            }
        }

        //----------------------------------------------------------------------
        // IsPolicyPresent.
        //----------------------------------------------------------------------

        [Test]
        public void IsPolicyPresent_MachinePolicyNull_UserPolicyNull()
        {
            var repository = new SampleRepository(
                CreateKey(),
                null,
                null);

            Assert.IsFalse(repository.IsPolicyPresent);
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyExists_UserPolicyNull()
        {
            var repository = new SampleRepository(
                CreateKey(),
                CreateMachinePolicyKey(),
                null);

            Assert.IsTrue(repository.IsPolicyPresent);
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyNull_UserPolicyExists()
        {
            var repository = new SampleRepository(
                CreateKey(),
                null,
                CreateUserPolicyKey());

            Assert.IsTrue(repository.IsPolicyPresent);
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyExists_UserPolicyExists()
        {
            var repository = new SampleRepository(
                CreateKey(),
                CreateMachinePolicyKey(),
                CreateUserPolicyKey());

            Assert.IsTrue(repository.IsPolicyPresent);
        }
    }
}
