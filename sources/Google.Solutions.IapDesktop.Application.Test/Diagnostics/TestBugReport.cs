//
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

using Google.Solutions.IapDesktop.Application.Diagnostics;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Diagnostics
{
    [TestFixture]
    public class TestBugReport
    {
        [Test]
        public void ToString_WhenExceptionIsNull_ThenToStringContainsVersionDetails()
        {
            var report = new BugReport(GetType(), new Exception());

            Assert.That(report.ToString(), Does.Contain(Environment.OSVersion.ToString()));
        }

        [Test]
        public void ToString_WhenExceptionIsNotNull_ThenToStringContainsNestedExceptionDetails()
        {
            var ex = new ApplicationException("outer", new NullReferenceException("inner"));
            var report = new BugReport(GetType(), ex);

            Assert.That(report.ToString(), Does.Contain("outer"));
            Assert.That(report.ToString(), Does.Contain("inner"));
            Assert.That(report.ToString(), Does.Contain("ApplicationException"));
            Assert.That(report.ToString(), Does.Contain("NullReferenceException"));
        }

        [Test]
        public void ToString_WhenExceptionIsLoaderException_ThenToStringContainsExceptionDetails()
        {
            var ex = new ReflectionTypeLoadException(
                new[] { typeof(BugReport) },
                new[]
                {
                    new ApplicationException("inner#1"),
                    new ApplicationException("inner#2")
                });
            var report = new BugReport(GetType(), ex);

            Assert.That(report.ToString(), Does.Contain("inner#1"));
            Assert.That(report.ToString(), Does.Contain("inner#2"));
        }

        [Test]
        public void ToString_WhenSourceWindowSetToControl_ThenToStringContainsWindowDetails()
        {
            using (var form = new Form()
            {
                Name = "TestForm",
            })
            {
                var report = new BugReport(GetType(), new ApplicationException("test"))
                {
                    SourceWindow = form
                };

                Assert.That(report.ToString(), Does.Contain("Window: TestForm (Form)"));
            }
        }
    }
}
