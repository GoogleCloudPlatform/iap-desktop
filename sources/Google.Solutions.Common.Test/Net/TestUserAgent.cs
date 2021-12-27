﻿//
// Copyright 2019 Google LLC
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

using Google.Solutions.Common.Net;
using NUnit.Framework;
using System;

namespace Google.Solutions.Common.Test.Net
{
    [TestFixture]
    public class TestUserAgent : CommonFixtureBase
    {
        [Test]
        public void WhenNoExtensionProvided_ToHeaderValueReturnsProperString()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95");

            Assert.AreEqual("WidgetTool/1.0 (Windows 95)", ua.ToHeaderValue());
            Assert.AreEqual(ua.ToHeaderValue(), ua.ToString());
        }

        [Test]
        public void WhenExtensionProvided_ToHeaderValueReturnsProperString()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95")
            {
                Extensions = "on-steroids"
            };

            Assert.AreEqual("WidgetTool/1.0 (Windows 95; on-steroids)", ua.ToHeaderValue());
            Assert.AreEqual(ua.ToHeaderValue(), ua.ToString());
        }
    }
}
