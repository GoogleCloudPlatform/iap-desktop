﻿//
// Copyright 2023 Google LLC
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

using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Traits
{
    [TestFixture]
    public class TestWindowsTrait : EquatableFixtureBase<WindowsTrait, ITrait>
    {
        protected override WindowsTrait CreateInstance()
        {
            return WindowsTrait.Instance;
        }

        //---------------------------------------------------------------------
        // DisplayName.
        //---------------------------------------------------------------------

        [Test]
        public void DisplayNameExpression()
        {
            Assert.AreEqual("isWindows()", WindowsTrait.Instance.DisplayName);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringReturnsExpression()
        {
            Assert.AreEqual("isWindows()", WindowsTrait.Instance.ToString());
        }

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExpressionIsNullOrEmpty_ThenTryParseReturnsFalse(
            [Values(" \t", "", null)] string expression)
        {
            Assert.IsFalse(WindowsTrait.TryParse(expression, out var _));
        }

        [Test]
        public void WhenExpressionIsValid_ThenTryParseReturnsTrue(
            [Values("isWindows()", " isWindows(  \n) \n\r\t ")] string expression)
        {
            Assert.IsTrue(WindowsTrait.TryParse(expression, out var trait));
            Assert.IsNotNull(trait);
        }

    }
}
