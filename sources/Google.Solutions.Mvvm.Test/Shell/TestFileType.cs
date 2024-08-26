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

using Google.Solutions.Mvvm.Shell;
using NUnit.Framework;
using System.IO;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileType
    {
        //---------------------------------------------------------------------
        // Lookup.
        //---------------------------------------------------------------------

        [Test]
        public void Lookup_WhenFileExtensionKnown_ThenLookupReturnsIcon(
            [Values(
                FileType.IconFlags.None,
                FileType.IconFlags.Open)] FileType.IconFlags size,
            [Values(
                FileAttributes.Normal,
                FileAttributes.ReadOnly,
                FileAttributes.ReparsePoint)] FileAttributes attributes)
        {
            using (var type = FileType.Lookup(
                "doesnotexist.txt",
                attributes,
                size))
            {
                Assert.IsNotNull(type.TypeName);
                Assert.AreNotEqual(string.Empty, type.TypeName);
                Assert.IsNotNull(type.FileIcon);
            }
        }

        [Test]
        public void Lookup_WhenFileExtensionUnknown_ThenLookupReturnsIcon(
            [Values(
                FileType.IconFlags.None,
                FileType.IconFlags.Open)] FileType.IconFlags size,
            [Values(
                FileAttributes.Normal,
                FileAttributes.Directory)] FileAttributes attributes)
        {
            using (var type = FileType.Lookup(
                "noextension",
                attributes,
                size))
            {
                Assert.IsNotNull(type.TypeName);
                Assert.AreNotEqual(string.Empty, type.TypeName);
                Assert.IsNotNull(type.FileIcon);
            }
        }

        //---------------------------------------------------------------------
        // IsFile.
        //---------------------------------------------------------------------

        [Test]
        public void IsFile_WhenFileHasDirectoryAttribute_ThenIsFileIsFalse()
        {
            using (var type = FileType.Lookup(
                "folder",
                FileAttributes.Directory,
                FileType.IconFlags.None))
            {
                Assert.IsFalse(type.IsFile);
            }
        }

        [Test]
        public void IsFile_WhenFileDoesNotHaveDirectoryAttribute_ThenIsFileIsTrue()
        {
            using (var type = FileType.Lookup(
                "folder",
                FileAttributes.ReadOnly | FileAttributes.Normal,
                FileType.IconFlags.None))
            {
                Assert.IsTrue(type.IsFile);
            }
        }
    }
}
