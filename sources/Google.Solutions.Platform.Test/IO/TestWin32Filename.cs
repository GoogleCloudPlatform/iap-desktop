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
        public void StripExtension_WhenFileHasExtension_ThenStripExtensionRemovesExtension()
        {
            Assert.That(Win32Filename.StripExtension("file.txt"), Is.EqualTo("file"));
            Assert.That(Win32Filename.StripExtension(".txt.tmp"), Is.EqualTo(".txt"));
            Assert.That(Win32Filename.StripExtension("file.txt.tmp"), Is.EqualTo("file.txt"));
        }

        [Test]
        public void StripExtension_WhenFileHasNoExtension_ThenStripExtensionRetainsName()
        {
            Assert.That(Win32Filename.StripExtension(".file"), Is.EqualTo(".file"));
            Assert.That(Win32Filename.StripExtension("file"), Is.EqualTo("file"));
        }

        //---------------------------------------------------------------------
        // IsValidFilename.
        //---------------------------------------------------------------------

        [Test]
        public void IsValidFilename_WhenFilenameIsDosDevice_ThenIsValidFilenameReturnsFalse(
            [Values("con", "Prn", "AUX", "NUL", "COM1", "COM9", "LPT1", "lpt9.txt")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void IsValidFilename_WhenFilenameContainsInvalidCharacters_ThenIsValidFilenameReturnsFalse(
            [Values("f<.txt", ":.txt", "\"file\".txt", "\\f", "?.txt", "*.*")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void IsValidFilename_WhenFilenameHasTrailingDot_ThenIsValidFilenameReturnsFalse(
            [Values("file.txt.")] string name)
        {
            Assert.IsFalse(Win32Filename.IsValidFilename(name));
        }

        [Test]
        public void IsValidFilename_WhenFilenameIsWin32Compliant_ThenIsValidFilenameReturnsTrue(
            [Values(".file.txt", "f", "null.txt")] string name)
        {
            Assert.IsTrue(Win32Filename.IsValidFilename(name));
        }

        //---------------------------------------------------------------------
        // EscapeFilename.
        //---------------------------------------------------------------------

        [Test]
        public void EscapeFilename_WhenFilenameIsWin32Compliant_ThenEscapeFilenameRetainsName()
        {
            Assert.That(Win32Filename.EscapeFilename("File.txt"), Is.EqualTo("File.txt"));
            Assert.That(Win32Filename.EscapeFilename("File with spaces"), Is.EqualTo("File with spaces"));
            Assert.That(Win32Filename.EscapeFilename(".dotfile"), Is.EqualTo(".dotfile"));
            Assert.That(Win32Filename.EscapeFilename("NULl.AUX"), Is.EqualTo("NULl.AUX"));
        }

        [Test]
        public void EscapeFilename_WhenFilenameNotWin32Compliant_ThenEscapeFilenameReturnsCompliantName()
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
