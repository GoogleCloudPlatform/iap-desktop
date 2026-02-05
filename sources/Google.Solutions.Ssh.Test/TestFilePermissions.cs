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

using NUnit.Framework;
using System;

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestFilePermissions
    {
        private const FilePermissions basePermissions =
            FilePermissions.OtherRead |
            FilePermissions.OwnerRead |
            FilePermissions.OtherWrite;

        //--------------------------------------------------------------------
        // IsXxx.
        //--------------------------------------------------------------------

        [Test]
        public void IsRegular()
        {
            Assert.That(basePermissions.IsRegular(), Is.False);
        }

        [Test]
        public void IsLink()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.SymbolicLink).IsLink(), Is.True);
        }

        [Test]
        public void IsDirectory()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.Directory).IsDirectory(), Is.True);
            Assert.That((basePermissions | FilePermissions.BlockSpecial).IsDirectory(), Is.False);
        }

        [Test]
        public void IsCharacterDevice()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.CharacterDevice).IsCharacterDevice(), Is.True);
        }

        [Test]
        public void IsBlockDevice()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.BlockSpecial).IsBlockDevice(), Is.True);
        }

        [Test]
        public void IsFifo()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.Fifo).IsFifo(), Is.True);
        }

        [Test]
        public void IsSocket()
        {
            Assert.That(basePermissions.IsLink(), Is.False);
            Assert.That((basePermissions | FilePermissions.Socket).IsSocket(), Is.True);
        }

        //--------------------------------------------------------------------
        // ToListFormat.
        //--------------------------------------------------------------------

        [Test]
        [TestCase("000", "----------")]
        [TestCase("777", "-rwxrwxrwx")]
        [TestCase("400", "-r--------")]
        [TestCase("644", "-rw-r--r--")]
        [TestCase("111", "---x--x--x")]
        public void ToListFormat_MapsPermissions(string octal, string expected)
        {
            var permissions = (FilePermissions)Convert.ToInt32(octal, 8);
            Assert.That(permissions.ToListFormat(), Is.EqualTo(expected));
        }

        [Test]
        [TestCase(FilePermissions.Directory, "d---------")]
        [TestCase(FilePermissions.SymbolicLink, "l---------")]
        [TestCase(FilePermissions.CharacterDevice, "c---------")]
        [TestCase(FilePermissions.BlockSpecial, "b---------")]
        [TestCase(FilePermissions.Socket, "s---------")]
        [TestCase(FilePermissions.Fifo, "p---------")]
        [TestCase(FilePermissions.Regular, "----------")]
        public void ToListFormat_MapsFileType(FilePermissions p, string expected)
        {
            Assert.That(p.ToListFormat(), Is.EqualTo(expected));
        }
    }
}
