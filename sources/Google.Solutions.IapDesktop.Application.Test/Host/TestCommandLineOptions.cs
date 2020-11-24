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

using Google.Solutions.IapDesktop.Application.Host;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestCommandLineOptions : FixtureBase
    {
        [Test]
        public void WhenArgsEmpty_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(new string[0]);
            
            Assert.IsFalse(options.IsLoggingEnabled);
            Assert.IsNull(options.StartupUrl);
        }

        [Test]
        public void WhenUrlSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1" });

            Assert.IsFalse(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void WhenUrlAndDebugSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1", "/debug" });

            Assert.IsTrue(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void WhenDebugAndUrlSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/debug", "/url", "iap-rdp:///project-1/us-central1-a/vm-1" });

            Assert.IsTrue(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void WhenUrlMissesArgument_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url" }));
        }

        [Test]
        public void WhenUrlIsInvalid_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url", "notaurl" }));
        }
        
        [Test]
        public void WhenInvalidFlagSpecified_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/" }));
        }

        [Test]
        public void WhenUrlAndInvalidFlagSpecified_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1", "/ debug" }));
        }
    }
}
