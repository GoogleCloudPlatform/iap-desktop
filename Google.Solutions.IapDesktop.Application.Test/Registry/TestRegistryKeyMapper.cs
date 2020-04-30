//
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

using Google.Solutions.IapDesktop.Application.Registry;
using Google.Solutions.IapDesktop.Application.Util;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Test.Registry
{
    [TestFixture]
    public class TestRegistryKeyMapper : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        class KeyWithData
        {
            [StringRegistryValue("String")]
            public string String { get; set; }

            [DwordRegistryValue("Dword")]
            public Int32 Dword { get; set; }

            [DwordRegistryValue("NullableDword")]
            public Int32? NullableDword { get; set; }

            [QwordRegistryValue("Qword")]
            public Int64 Qword { get; set; }

            [QwordRegistryValue("NullableQword")]
            public Int64? NullableQword { get; set; }

            [BoolRegistryValue("Bool")]
            public bool Bool { get; set; }

            [BoolRegistryValue("NullableBool")]
            public bool? NullableBool { get; set; }
        }

        [Test]
        public void WhenValidDataIsStored_SameDataReadOnLoad()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            var original = new KeyWithData()
            {
                String = "test",
                Dword = 42,
                NullableDword = 45,
                Qword = 99999999999999999L,
                NullableQword = null,
                Bool = true,
                NullableBool = true
            };
            new RegistryKeyMapper<KeyWithData>().MapObjectToKey(original, registryKey);

            var copy = new RegistryKeyMapper<KeyWithData>().MapKeyToObject(registryKey);
            Assert.AreEqual(original.String, copy.String);
            Assert.AreEqual(original.Dword, copy.Dword);
            Assert.AreEqual(original.NullableDword, copy.NullableDword);
            Assert.AreEqual(original.Qword, copy.Qword);
            Assert.AreEqual(original.NullableQword, copy.NullableQword);
            Assert.AreEqual(original.Bool, copy.Bool);
            Assert.AreEqual(original.NullableBool, copy.NullableBool);
        }

        [Test]
        public void WhenNullDataIsStored_SameDataReadOnLoad()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            var original = new KeyWithData()
            {
                String = null,
                NullableDword = null
            };
            new RegistryKeyMapper<KeyWithData>().MapObjectToKey(original, registryKey);

            var copy = new RegistryKeyMapper<KeyWithData>().MapKeyToObject(registryKey);
            Assert.AreEqual(original.String, copy.String);
            Assert.AreEqual(original.Dword, copy.Dword);
            Assert.AreEqual(original.NullableDword, copy.NullableDword);
        }

        class KeyWithIncompatibleValueKind
        {
            [DwordRegistryValue("StringAsDword")]
            public string StringAsDword { get; set; }

        }

        [Test]
        public void WhenValueKindIsNotCompatibleWithPropertyType_InvalidCastExceptionThrownOnStore()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            var original = new KeyWithIncompatibleValueKind()
            {
                StringAsDword = "sad"
            };

            Assert.Throws<InvalidCastException>(() =>
            {
                new RegistryKeyMapper<KeyWithIncompatibleValueKind>().MapObjectToKey(original, registryKey);
            });
        }

        [Test]
        public void WhenValueKindIsNotCompatibleWithPropertyType_InvalidCastExceptionThrownOnLoad()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            Assert.Throws<InvalidCastException>(() =>
            {
                new RegistryKeyMapper<KeyWithIncompatibleValueKind>().MapKeyToObject(registryKey);
            });
        }

        class KeyWithString
        {
            [StringRegistryValue("Data")]
            public string Data { get; set; }
        }

        class KeyWithDword
        {
            [DwordRegistryValue("Data")]
            public Int32 Data { get; set; }
        }

        [Test]
        public void WhenLoadingAsDifferentType_InvalidCastExceptionThrownOnStore()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            new RegistryKeyMapper<KeyWithString>().MapObjectToKey(
                new KeyWithString()
                {
                    Data = "dasd"
                },
                registryKey);

            Assert.Throws<InvalidCastException>(() =>
            {
                new RegistryKeyMapper<KeyWithDword>().MapKeyToObject(registryKey);
            });
        }

        public class KeyWithSecureString
        {
            [SecureStringRegistryValue("secure", DataProtectionScope.CurrentUser)]
            public SecureString Secure { get; set; }
        }

        [Test]
        public void WhenSecureStringStored_StringCanBeDecrypted()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            var original = new KeyWithSecureString()
            {
                Secure = SecureStringExtensions.FromClearText("secure!!!")
            };

            new RegistryKeyMapper<KeyWithSecureString>().MapObjectToKey(original, registryKey);

            var copy = new RegistryKeyMapper<KeyWithSecureString>().MapKeyToObject(registryKey);

            Assert.AreEqual(
                "secure!!!",
                copy.Secure.AsClearText());
        }

        [Test]
        public void WhenSecureStringIsNull_RemainsNull()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            var original = new KeyWithSecureString()
            {
            };

            new RegistryKeyMapper<KeyWithSecureString>().MapObjectToKey(original, registryKey);

            var copy = new RegistryKeyMapper<KeyWithSecureString>().MapKeyToObject(registryKey);

            Assert.IsNull(copy.Secure);
        }

        [Test]
        public void WhenSecureStringIsDeleted_RemainsNull()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            new RegistryKeyMapper<KeyWithSecureString>().MapObjectToKey(
                new KeyWithSecureString()
                {
                    Secure = SecureStringExtensions.FromClearText("secure!!!")
                },
                registryKey);
            new RegistryKeyMapper<KeyWithSecureString>().MapObjectToKey(
                new KeyWithSecureString()
                {
                    Secure = null
                },
                registryKey);

            var copy = new RegistryKeyMapper<KeyWithSecureString>().MapKeyToObject(registryKey);

            Assert.IsNull(copy.Secure);
        }
    }
}

