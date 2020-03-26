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
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace Google.Solutions.IapDesktop.Application.Test.Registry
{
    [TestFixture]
    public class TestRegistryBinder
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        class EmptyKey
        {
        }

        [Test]
        public void WhenClassHasNoValues_ThenValueNamesReturnsEmptyList()
        {
            Assert.IsFalse(new RegistryBinder<EmptyKey>().ValueNames.Any());
        }


        class KeyWithValueWithoutName
        {
            [StringRegistryValue(null)]
            public string IgnoredNullValue { get; }
        }

        [Test]
        public void WhenClassHasValueWithoutName_ThenValueIsIgnored()
        {
            Assert.IsFalse(new RegistryBinder<EmptyKey>().ValueNames.Any());
        }

        class KeyWithValues
        {
            [StringRegistryValue("str")]
            public string String { get; } = "test";

            [DwordRegistryValue("dword")]
            public int Dword { get; } = 42;
        }

        [Test]
        public void WhenClassHasValues_ThenValueNamesReturnsNames()
        {
            CollectionAssert.AreEqual(
                new[] { "str", "dword" },
                new RegistryBinder<KeyWithValues>().ValueNames);
        }

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
            new RegistryBinder<KeyWithData>().Store(original, registryKey);

            var copy = new RegistryBinder<KeyWithData>().Load(registryKey);
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
            new RegistryBinder<KeyWithData>().Store(original, registryKey);

            var copy = new RegistryBinder<KeyWithData>().Load(registryKey);
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
                new RegistryBinder<KeyWithIncompatibleValueKind>().Store(original, registryKey);
            });
        }

        [Test]
        public void WhenValueKindIsNotCompatibleWithPropertyType_InvalidCastExceptionThrownOnLoad()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            Assert.Throws<InvalidCastException>(() =>
            {
                new RegistryBinder<KeyWithIncompatibleValueKind>().Load(registryKey);
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

            new RegistryBinder<KeyWithString>().Store(
                new KeyWithString()
                {
                    Data = "dasd"
                },
                registryKey);

            Assert.Throws<InvalidCastException>(() =>
            {
                new RegistryBinder<KeyWithDword>().Load(registryKey);
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

            new RegistryBinder<KeyWithSecureString>().Store(original, registryKey);

            var copy = new RegistryBinder<KeyWithSecureString>().Load(registryKey);

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

            new RegistryBinder<KeyWithSecureString>().Store(original, registryKey);

            var copy = new RegistryBinder<KeyWithSecureString>().Load(registryKey);

            Assert.IsNull(copy.Secure);
        }

        [Test]
        public void WhenSecureStringIsDeleted_RemainsNull()
        {
            this.hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var registryKey = hkcu.CreateSubKey(TestKeyPath);

            new RegistryBinder<KeyWithSecureString>().Store(
                new KeyWithSecureString()
                {
                    Secure = SecureStringExtensions.FromClearText("secure!!!")
                },
                registryKey);
            new RegistryBinder<KeyWithSecureString>().Store(
                new KeyWithSecureString()
                {
                    Secure = null
                },
                registryKey);

            var copy = new RegistryBinder<KeyWithSecureString>().Load(registryKey);

            Assert.IsNull(copy.Secure);
        }
    }
}

