//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Input;
using NUnit.Framework;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Input
{
    [TestFixture]
    public class TestKeyboardLayout
    {
        //---------------------------------------------------------------------
        // TryMapVirtualKey.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCharUnmapped_ThenTryMapVirtualKeyReturnsFalse()
        {
            Assert.IsFalse(KeyboardLayout.Current.TryMapVirtualKey('Ä', out var _));
        }

        [Test]
        public void WhenCharMapped_ThenTryMapVirtualKeyReturnsTrue()
        {
            Assert.IsTrue(KeyboardLayout.Current.TryMapVirtualKey('A', out var vk));
            Assert.AreEqual(Keys.A | Keys.Shift, vk);

            Assert.IsTrue(KeyboardLayout.Current.TryMapVirtualKey('a', out vk));
            Assert.AreEqual(Keys.A, vk);

            Assert.IsTrue(KeyboardLayout.Current.TryMapVirtualKey('1', out vk));
            Assert.AreEqual(Keys.D1, vk);
        }

        //---------------------------------------------------------------------
        // TranslateModifiers.
        //---------------------------------------------------------------------

        [Test]
        public void TranslateModifiers()
        {
            CollectionAssert.AreEqual(
                new[] { Keys.ShiftKey, Keys.A },
                KeyboardLayout.TranslateModifiers(Keys.A | Keys.Shift));

            CollectionAssert.AreEqual(
                new[] { Keys.B },
                KeyboardLayout.TranslateModifiers(Keys.B));

            CollectionAssert.AreEqual(
                new[] { Keys.ShiftKey, Keys.ControlKey, Keys.Menu, Keys.Delete },
                KeyboardLayout.TranslateModifiers(
                    Keys.Delete | Keys.Shift | Keys.Alt | Keys.Control));
        }

        //---------------------------------------------------------------------
        // ToScanCodes.
        //---------------------------------------------------------------------

        [Test]
        public void ToScanCodes()
        {
            var scanCodes = KeyboardLayout.Current
                .ToScanCodes(Keys.A | Keys.Shift)
                .ToList();
            Assert.AreEqual(2, scanCodes.Count);
        }
    }
}
