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
using System;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileTypeCache
    {
        //---------------------------------------------------------------------
        // Lookup.
        //---------------------------------------------------------------------

        [Test]
        public void Lookup_WhenDirectory_ThenLookupUsesCache()
        {
            using (var cache = new FileTypeCache())
            {
                var type1 = cache.Lookup("dir-1", FileAttributes.Directory, FileType.IconFlags.None);
                var type2 = cache.Lookup("dir-2", FileAttributes.Directory, FileType.IconFlags.None);

                Assert.AreSame(type1, type2);
                Assert.AreEqual(1, cache.CacheSize);
            }
        }

        [Test]
        public void Lookup_WhenFilesHaveSameExtension_ThenLookupUsesCache()
        {
            using (var cache = new FileTypeCache())
            {
                var type1 = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.None);
                var type2 = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.None);

                Assert.AreSame(type1, type2);
                Assert.AreEqual(1, cache.CacheSize);
            }
        }

        [Test]
        public void Lookup_WhenFilesDifferBySize_ThenLookupSkipsCache()
        {
            using (var cache = new FileTypeCache())
            {
                var type1 = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.None);
                var type2 = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.Open);

                Assert.AreNotSame(type1, type2);
                Assert.AreEqual(2, cache.CacheSize);
            }
        }

        [Test]
        public void Lookup_WhenFilesDifferByAttribute_ThenLookupSkipsCache()
        {
            using (var cache = new FileTypeCache())
            {
                var type1 = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.None);
                var type2 = cache.Lookup("test-1.txt", FileAttributes.ReadOnly, FileType.IconFlags.None);

                Assert.AreNotSame(type1, type2);
                Assert.AreEqual(2, cache.CacheSize);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose_WhenDisposed_FileTypesAndIconsAreDisposed()
        {
            var cache = new FileTypeCache();
            var type = cache.Lookup("test-1.txt", FileAttributes.Normal, FileType.IconFlags.None);
            cache.Dispose();

            Assert.Throws<ArgumentException>(() => ((Bitmap)type.FileIcon).GetHicon());
        }
    }
}
