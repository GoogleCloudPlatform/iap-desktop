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

using Google.Solutions.Platform.Interop;
using NUnit.Framework;

namespace Google.Solutions.Platform.Test.Interop
{
    [TestFixture]
    public class TestWindowsFilename
    {
        //---------------------------------------------------------------------
        // StripExtension.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFileHasExtension_ThenStripExtensionRemovesExtension()
        {
            Assert.AreEqual("file", Win32Filename.StripExtension("file.txt"));
            Assert.AreEqual(".txt", Win32Filename.StripExtension(".txt.tmp"));
            Assert.AreEqual("file.txt", Win32Filename.StripExtension("file.txt.tmp"));
        }

        [Test]
        public void WhenFileHasNoExtension_ThenStripExtensionRetainsName()
        {
            Assert.AreEqual(".file", Win32Filename.StripExtension(".file"));
            Assert.AreEqual("file", Win32Filename.StripExtension("file"));
        }

        //---------------------------------------------------------------------
        // IsValidFilename.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFilenameIsDosDevice_ThenIsValidFilenameReturnsFalse(
            [Values("con", "Prn", "AUX", "NUL", "COM1", "COM9", "LPT1", "lpt9.txt")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameContainsInvalidCharacters_ThenIsValidFilenameReturnsFalse(
            [Values("f<.txt", ":.txt", "\"file\".txt", "\\f", "?.txt", "*.*")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameHasTrailingDot_ThenIsValidFilenameReturnsFalse(
            [Values("file.txt.")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameIsWin32Compliant_ThenIsValidFilenameReturnsTrue(
            [Values(".file.txt", "f", "null.txt")] string name)
        {
            Assert.IsTrue(Win32Filename.IsValidFilename(name));
        }

        //---------------------------------------------------------------------
        // EscapeFilename.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFilenameIsWin32Compliant_ThenEscapeFilenameRetainsName()
        {
            Assert.AreEqual("File.txt", Win32Filename.EscapeFilename("File.txt"));
            Assert.AreEqual("File with spaces", Win32Filename.EscapeFilename("File with spaces"));
            Assert.AreEqual(".dotfile", Win32Filename.EscapeFilename(".dotfile"));
            Assert.AreEqual("NULl.AUX", Win32Filename.EscapeFilename("NULl.AUX"));
        }

        [Test]
        public void WhenFilenameNotWin32Compliant_ThenEscapeFilenameReturnsCompliantName()
        {
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename(".")));
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename("file.")));
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename("NUL")));
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename("NUL.")));
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename("AUX.txt")));
            Assert.IsTrue(Win32Filename.IsValidFilename(Win32Filename.EscapeFilename("\"file\"")));
        }
    }
}
