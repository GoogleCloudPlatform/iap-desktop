//
// Copyright 2021 Google LLC
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

using Google.Apis.Iam.v1.Data;
using Microsoft.Win32;
using NUnit.Framework;
using System;

namespace Google.Solutions.Testing.Apis.Platform
{
    public sealed class RegistryKeyPath : IDisposable
    {
        private static readonly RegistryKey Hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Default);

        public string Path { get; }

        private RegistryKeyPath(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Create a temporary registry key based on the
        /// name of the currently executing test.
        /// </summary>
        public static RegistryKeyPath ForCurrentTest()
        {
            var currentTest = TestContext.CurrentContext.Test;
            var path = new RegistryKeyPath(
                @"Software\Google\__Test\" +
                $"{currentTest.ClassName}.{currentTest.Name}");
            path.Delete();
            return path;
        }

        public void Delete()
        {
            Hkcu.DeleteSubKeyTree(this.Path, false);
        }

        public RegistryKey CreateKey()
        {
            return Hkcu.CreateSubKey(this.Path);
        }

        public void Dispose()
        {
            Delete();
        }
    }
}
