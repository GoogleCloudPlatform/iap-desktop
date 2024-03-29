﻿//
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

using Microsoft.Win32;
using NUnit.Framework;

namespace Google.Solutions.Settings.Test
{
    public abstract class RegistrySettingsStoreBase
    {
        private const string KeyPath = @"Software\Google\__Test";
        private const string PolicyKeyPath = @"Software\Google\__TestPolicy";

        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            this.hkcu.DeleteSubKeyTree(KeyPath, false);
            this.hkcu.DeleteSubKeyTree(PolicyKeyPath, false);
        }

        protected RegistrySettingsStore CreateSettingsKey()
        {
            return new RegistrySettingsStore(this.hkcu.CreateSubKey(KeyPath));
        }

        protected RegistrySettingsStore CreatePolicySettingsKey()
        {
            return new RegistrySettingsStore(this.hkcu.CreateSubKey(PolicyKeyPath));
        }
    }
}
