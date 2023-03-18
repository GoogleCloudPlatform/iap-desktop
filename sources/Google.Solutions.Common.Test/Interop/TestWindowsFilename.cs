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

using Google.Solutions.Common.Interop;
using NUnit.Framework;

namespace Google.Solutions.Common.Test.Interop
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
            Assert.AreEqual("file", WindowsFilename.StripExtension("file.txt"));
            Assert.AreEqual(".txt", WindowsFilename.StripExtension(".txt.tmp"));
            Assert.AreEqual("file.txt", WindowsFilename.StripExtension("file.txt.tmp"));
        }

        [Test]
        public void WhenFileHasNoExtension_ThenStripExtensionRetainsName()
        {
            Assert.AreEqual(".file", WindowsFilename.StripExtension(".file"));
            Assert.AreEqual("file", WindowsFilename.StripExtension("file"));
        }

        //---------------------------------------------------------------------
        // IsValidFilename.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFilenameIsDosDevice_ThenIsValidFilenameReturnsFalse(
            [Values("con", "Prn", "AUX", "NUL", "COM1", "COM9", "LPT1", "lpt9.txt")] string name)
        {
            Assert.IsFalse(WindowsFilename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameContainsInvalidCharacters_ThenIsValidFilenameReturnsFalse(
            [Values("f<.txt", ":.txt", "\"file\".txt", "\\f", "?.txt", "*.*")] string name)
        {
            Assert.IsFalse(WindowsFilename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameHasTrailingDot_ThenIsValidFilenameReturnsFalse(
            [Values("file.txt.")] string name)
        {
            Assert.IsFalse(WindowsFilename.IsValidFilename(name));
        }

        [Test]
        public void WhenFilenameIsWin32Compliant_ThenIsValidFilenameReturnsTrue(
            [Values(".file.txt", "f", "null.txt")] string name)
        {
            Assert.IsTrue(WindowsFilename.IsValidFilename(name));
        }

        //---------------------------------------------------------------------
        // EscapeFilename.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFilenameIsWin32Compliant_ThenEscapeFilenameRetainsName()
        {
            Assert.AreEqual("File.txt", WindowsFilename.EscapeFilename("File.txt"));
            Assert.AreEqual("File with spaces", WindowsFilename.EscapeFilename("File with spaces"));
            Assert.AreEqual(".dotfile", WindowsFilename.EscapeFilename(".dotfile"));
            Assert.AreEqual("NULl.AUX", WindowsFilename.EscapeFilename("NULl.AUX"));
        }

        [Test]
        public void WhenFilenameNotWin32Compliant_ThenEscapeFilenameReturnsCompliantName()
        {
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename(".")));
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename("file.")));
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename("NUL")));
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename("NUL.")));
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename("AUX.txt")));
            Assert.IsTrue(WindowsFilename.IsValidFilename(WindowsFilename.EscapeFilename("\"file\"")));
        }
    }
}
