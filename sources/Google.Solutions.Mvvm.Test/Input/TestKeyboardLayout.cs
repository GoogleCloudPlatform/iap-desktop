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
        public void TryMapVirtualKey_WhenCharUnmapped()
        {
            Assert.That(KeyboardLayout.Current.TryMapVirtualKey('Ä', out var _), Is.False);
        }

        [Test]
        public void TryMapVirtualKey_WhenCharMapped()
        {
            Assert.That(KeyboardLayout.Current.TryMapVirtualKey('A', out var vk), Is.True);
            Assert.That(vk, Is.EqualTo(Keys.A | Keys.Shift));

            Assert.That(KeyboardLayout.Current.TryMapVirtualKey('a', out vk), Is.True);
            Assert.That(vk, Is.EqualTo(Keys.A));

            Assert.That(KeyboardLayout.Current.TryMapVirtualKey('1', out vk), Is.True);
            Assert.That(vk, Is.EqualTo(Keys.D1));
        }

        //---------------------------------------------------------------------
        // TranslateModifiers.
        //---------------------------------------------------------------------

        [Test]
        public void TranslateModifiers()
        {
            Assert.That(
                KeyboardLayout.TranslateModifiers(Keys.A | Keys.Shift), Is.EqualTo(new[] { Keys.ShiftKey, Keys.A }).AsCollection);

            Assert.That(
                KeyboardLayout.TranslateModifiers(Keys.B), Is.EqualTo(new[] { Keys.B }).AsCollection);

            Assert.That(
                KeyboardLayout.TranslateModifiers(
                    Keys.Delete | Keys.Shift | Keys.Alt | Keys.Control), Is.EqualTo(new[] { Keys.ControlKey, Keys.Menu, Keys.ShiftKey, Keys.Delete }).AsCollection);
        }

        //---------------------------------------------------------------------
        // ToScanCodes.
        //---------------------------------------------------------------------

        [Test]
        public void ToScanCodes()
        {
            var scanCodes = KeyboardLayout.ToScanCodes(Keys.A | Keys.Shift)
                .ToList();
            Assert.That(scanCodes.Count, Is.EqualTo(2));
        }
    }
}
