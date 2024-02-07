//
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

using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Apis.Client;
using Google.Solutions.Common.Diagnostics;
using NUnit.Framework;
using System;

namespace Google.Solutions.Apis.Test.Client
{
    [TestFixture]
    public class TestUserAgent
    {
        //---------------------------------------------------------------------
        // Basic properties.
        //---------------------------------------------------------------------

        [Test]
        public void Product()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95");
            Assert.AreEqual("WidgetTool", ua.Product);
        }

        [Test]
        public void Version()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95");
            Assert.AreEqual(new Version(1, 0), ua.Version);
        }

        [Test]
        public void OsVersion()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95");
            Assert.AreEqual("Windows 95", ua.Platform);
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void ToStringIncludesPlatform()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95");

            Assert.AreEqual(
                $"WidgetTool/1.0 (Windows 95) CLR/{ClrVersion.Version}",
                ua.ToString());
            Assert.AreEqual(ua.ToString(), ua.ToString());
        }

        [Test]
        public void ToStringIncludesExtension()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95")
            {
                Extensions = "on-steroids"
            };

            Assert.AreEqual(
                $"WidgetTool/1.0 (Windows 95; on-steroids) CLR/{ClrVersion.Version}",
                ua.ToString());
            Assert.AreEqual(ua.ToString(), ua.ToString());
        }

        //---------------------------------------------------------------------
        // ToApplicationName.
        //---------------------------------------------------------------------

        [Test]
        public void ToApplicationNameDoesNotIncludeExtensionsOrClrVersion()
        {
            var ua = new UserAgent("WidgetTool", new Version(1, 0), "Windows 95")
            {
                Extensions = "on-steroids"
            };

            var service = new ComputeService(new BaseClientService.Initializer()
            {
                ApplicationName = ua.ToApplicationName()
            });

            Assert.AreEqual("WidgetTool/1.0", service.ApplicationName);
        }
    }
}
