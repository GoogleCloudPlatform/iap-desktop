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

namespace Google.Solutions.Ssh.Test
{
    [TestFixture]
    public class TestFilePermissions
    {
        private const FilePermissions basePermissions =
            FilePermissions.OtherRead |
            FilePermissions.OwnerRead |
            FilePermissions.OtherWrite;

        [Test]
        public void IsRegular()
        {
            Assert.IsFalse(basePermissions.IsRegular());
        }

        [Test]
        public void IsLink()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.SymbolicLink).IsLink());
        }

        [Test]
        public void IsDirectory()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.Directory).IsDirectory());
            Assert.IsFalse((basePermissions | FilePermissions.BlockSpecial).IsDirectory());
        }

        [Test]
        public void IsCharacterDevice()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.CharacterDevice).IsCharacterDevice());
        }

        [Test]
        public void IsBlockDevice()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.BlockSpecial).IsBlockDevice());
        }

        [Test]
        public void IsFifo()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.Fifo).IsFifo());
        }

        [Test]
        public void IsSocket()
        {
            Assert.IsFalse(basePermissions.IsLink());
            Assert.IsTrue((basePermissions | FilePermissions.Socket).IsSocket());
        }
    }
}
