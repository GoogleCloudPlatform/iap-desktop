﻿//
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


using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;
using System;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestBound
    {
        //-----------------------------------------------------------
        // HasValue.
        //-----------------------------------------------------------

        [Test]
        public void WhenInitialized_ThenHasValueIsTrue()
        {
            var bound = new Bound<string>();

            Assert.IsFalse(bound.HasValue);
            bound.Value = "test";
            Assert.IsTrue(bound.HasValue);
        }

        //-----------------------------------------------------------
        // Value.
        //-----------------------------------------------------------

        [Test]
        public void WhenNotInitialized_ThenGetValueThrowsException()
        {
            var bound = new Bound<string>();
            Assert.Throws<InvalidOperationException>(() => bound.Value.ToString());
        }

        [Test]
        public void WhenInitialized_ThenGetValueReturns()
        {
            var bound = new Bound<string>
            {
                Value = "test"
            };

            Assert.AreEqual("test", bound.Value);
        }

        [Test]
        public void WhenInitialized_ThenSetValueThrowsException()
        {
            var bound = new Bound<string>
            {
                Value = "test"
            };

            Assert.Throws<InvalidOperationException>(() => bound.Value = "new value");
        }

        //-----------------------------------------------------------
        // Conversion operator.
        //-----------------------------------------------------------

        [Test]
        public void WhenNotInitialized_ThenGetConversionThrowsException()
        {
            var bound = new Bound<string>();
            Assert.Throws<InvalidOperationException>(() => ((string)bound).ToString());
        }

        [Test]
        public void WhenInitialized_ThenGetConversionReturns()
        {
            var bound = new Bound<string>
            {
                Value = "test"
            };

            string stringValue = bound.Value;
            Assert.AreEqual("test", stringValue);
        }

        //-----------------------------------------------------------
        // ToString operator.
        //-----------------------------------------------------------

        [Test]
        public void WhenNotInitialized_ThenToStringReturnsEmpty()
        {
            var bound = new Bound<string>();
            Assert.AreEqual("", bound.ToString());
        }

        [Test]
        public void WhenInitialized_ThenToStringReturnsValue()
        {
            var bound = new Bound<string>
            {
                Value = "test"
            };
            Assert.AreEqual("test", bound.ToString());
        }
    }
}
