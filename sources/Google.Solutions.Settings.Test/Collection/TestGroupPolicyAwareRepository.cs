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
using Google.Solutions.Testing.Apis.Platform;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Settings.Test.Collection
{
    [TestFixture]
    public class TestGroupPolicyAwareRepository
    {
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
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            {
                var repository = new SampleRepository(
                    settingsPath.CreateKey(),
                    null,
                    null);

                Assert.That(repository.IsPolicyPresent, Is.False);
            }
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyExists_UserPolicyNull()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            {
                var repository = new SampleRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    null);

                Assert.That(repository.IsPolicyPresent, Is.True);
            }
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyNull_UserPolicyExists()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SampleRepository(
                    settingsPath.CreateKey(),
                    null,
                    userPolicyPath.CreateKey());

                Assert.That(repository.IsPolicyPresent, Is.True);
            }
        }

        [Test]
        public void IsPolicyPresent_MachinePolicyExists_UserPolicyExists()
        {
            using (var settingsPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.Settings))
            using (var machinePolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.MachinePolicy))
            using (var userPolicyPath = RegistryKeyPath.ForCurrentTest(RegistryKeyPath.KeyType.UserPolicy))
            {
                var repository = new SampleRepository(
                    settingsPath.CreateKey(),
                    machinePolicyPath.CreateKey(),
                    userPolicyPath.CreateKey());

                Assert.That(repository.IsPolicyPresent, Is.True);
            }
        }
    }
}
