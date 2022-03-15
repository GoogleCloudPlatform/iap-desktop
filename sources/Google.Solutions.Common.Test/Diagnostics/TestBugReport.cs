﻿//
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

using Google.Solutions.Common.Diagnostics;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Diagnostics
{
    [TestFixture]
    public class TestBugReport : CommonFixtureBase
    {
        [Test]
        public void WhenExceptionIsNull_ThenToStringContainsVersionDetails()
        {
            var report = new BugReport();

            StringAssert.Contains(Environment.OSVersion.ToString(), report.ToString());
        }

        [Test]
        public void WhenExceptionIsNotNull_ThenToStringContainsNestedExceptionDetails()
        {
            var ex = new ApplicationException("outer", new NullReferenceException("inner"));
            var report = new BugReport(ex);

            StringAssert.Contains("outer", report.ToString());
            StringAssert.Contains("inner", report.ToString());
            StringAssert.Contains("ApplicationException", report.ToString());
            StringAssert.Contains("NullReferenceException", report.ToString());
        }

        [Test]
        public void WhenExceptionIsLoaderException_ThenToStringContainsExceptionDetails()
        {
            var ex = new ReflectionTypeLoadException(
                new[] { typeof(BugReport) },
                new[] 
                { 
                    new ApplicationException("inner#1"),
                    new ApplicationException("inner#2")
                });
            var report = new BugReport(ex);

            StringAssert.Contains("inner#1", report.ToString());
            StringAssert.Contains("inner#2", report.ToString());
        }
    }
}
