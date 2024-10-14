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


using Google.Solutions.Settings.ComponentModel;
using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Globalization;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Google.Solutions.Settings.Test.ComponentModel
{
    [TestFixture]
    public class TestEnumDisplayNameConverter
    {
        private enum EnumWithoutValues { }

        private enum EnumWithDuplicateDescriptions
        {
            [Description("one")]
            One,

            [Description("one")]
            Anotherone,
        }

        private enum EnumWithDescriptions
        {
            [Description("eins")]
            One,

            [Description("zwei")]
            Two,

            Many
        }

        private enum EnumWithoutDescriptions
        {
            Yes,
            No
        }

        //----------------------------------------------------------------------
        // Constructor.
        //----------------------------------------------------------------------

        [Test]
        public void Constructor_WhenEnumIsEmpty()
        {
            _ = new EnumDisplayNameConverter(typeof(EnumWithoutValues));
        }

        [Test]
        public void Constructor_WhenEnumLacksDescriptions()
        {
            _ = new EnumDisplayNameConverter(typeof(EnumWithoutDescriptions));
        }

        [Test]
        public void Constructor_WhenEnumDescriptionsNotUnique()
        {
            Assert.Throws<ArgumentException>(
                () => new EnumDisplayNameConverter(typeof(EnumWithDuplicateDescriptions)));
        }

        //----------------------------------------------------------------------
        // CanConvertTo.
        //----------------------------------------------------------------------

        [Test]
        public void CanConvertTo_WhenString()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.IsTrue(converter.CanConvertTo(typeof(string)));
        }

        [Test]
        public void CanConvertTo_WhenInt()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.IsFalse(converter.CanConvertTo(typeof(int)));
        }

        //----------------------------------------------------------------------
        // ConvertTo.
        //----------------------------------------------------------------------

        [Test]
        public void ConvertTo_WhenDescriptionPresent()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.AreEqual(
                "eins",
                converter.ConvertTo(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    EnumWithDescriptions.One,
                    typeof(string)));
        }

        [Test]
        public void ConvertTo_WhenDescriptionNotPresent()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.AreEqual(
                "Many",
                converter.ConvertTo(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    EnumWithDescriptions.Many,
                    typeof(string)));
        }

        [Test]
        public void ConvertTo_WhenDestinationTypeIsInt()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.Throws<NotSupportedException>(
                () => converter.ConvertTo(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    EnumWithDescriptions.Many,
                    typeof(int)));
        }

        //----------------------------------------------------------------------
        // ConvertFrom.
        //----------------------------------------------------------------------

        [Test]
        public void ConvertFrom_WhenDescriptionNotFound()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => converter.ConvertFrom(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    "invalid"));
        }

        [Test]
        public void ConvertFrom_WhenValueIsNull()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.Throws<ArgumentNullException>(
                () => converter.ConvertFrom(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    null));
        }

        [Test]
        public void ConvertFrom()
        {
            var converter = new EnumDisplayNameConverter(typeof(EnumWithDescriptions));
            Assert.AreEqual(
                EnumWithDescriptions.Two,
                converter.ConvertFrom(
                    new Mock<ITypeDescriptorContext>().Object,
                    CultureInfo.InvariantCulture,
                    "zwei"));
        }
    }
}
