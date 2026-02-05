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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test.Host
{
    [TestFixture]
    public class TestCommandLineOptions : ApplicationFixtureBase
    {
        [Test]
        public void Parse_WhenArgsEmpty_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(Array.Empty<string>());

            Assert.IsFalse(options.IsLoggingEnabled);
            Assert.IsNull(options.StartupUrl);
            Assert.IsNull(options.Profile);
        }

        //---------------------------------------------------------------------
        // /url.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenUrlSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1" });

            Assert.IsFalse(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void Parse_WhenUrlAndDebugSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1", "/debug" });

            Assert.IsTrue(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void Parse_WhenDebugAndUrlSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/debug", "/url", "iap-rdp:///project-1/us-central1-a/vm-1" });

            Assert.IsTrue(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
        }

        [Test]
        public void Parse_WhenUrlMissesArgument_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url" }));
        }

        [Test]
        public void Parse_WhenUrlIsInvalid_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url", "notaurl" }));
        }

        [Test]
        public void Parse_WhenInvalidFlagSpecified_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/" }));
        }

        [Test]
        public void Parse_WhenUrlAndInvalidFlagSpecified_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/url", "iap-rdp:///project-1/us-central1-a/vm-1", "/ debug" }));
        }

        //---------------------------------------------------------------------
        // /profile.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenProfileSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/profile", "profile-1" });

            Assert.That(options.Profile, Is.EqualTo("profile-1"));
            Assert.IsFalse(options.IsPostInstall);
        }

        [Test]
        public void Parse_WhenProfileAndDebugAndUrlSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] {
                    "/url", "iap-rdp:///project-1/us-central1-a/vm-1",
                    "/debug",
                    "/profile", "profile-1"});

            Assert.IsTrue(options.IsLoggingEnabled);
            Assert.IsNotNull(options.StartupUrl);
            Assert.That(options.Profile, Is.EqualTo("profile-1"));
        }

        [Test]
        public void Parse_WhenProfileMissesArgument_ThenParseRaisesInvalidCommandLineException()
        {
            Assert.Throws<InvalidCommandLineException>(
                () => CommandLineOptions.Parse(
                    new[] { "/profile" }));
        }

        [Test]
        public void Parse_WhenProfileIsEmpty_ThenProfileIsIgnored()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/profile", " " });
            Assert.IsNull(options.Profile);

        }

        //---------------------------------------------------------------------
        // /postinstall.
        //---------------------------------------------------------------------

        [Test]
        public void Parse_WhenPostInstallSpecified_ThenParseReturnsValidOptions()
        {
            var options = CommandLineOptions.Parse(
                new[] { "/profile", "profile-1", "/postinstall", "/debug" });

            Assert.IsTrue(options.IsPostInstall);
        }

        //---------------------------------------------------------------------
        // ToString
        //---------------------------------------------------------------------

        [Test]
        public void ToString_WhenAllOptionsClear_ThenToStringReturnsEmptyString()
        {
            var options = new CommandLineOptions();
            Assert.That(options.ToString(), Is.EqualTo(string.Empty));
        }

        [Test]
        public void ToString_WhenOptionsSpecified_ThenToStringReturnsQuotedString()
        {
            var options = new CommandLineOptions()
            {
                IsLoggingEnabled = true,
                Profile = "some profile",
                StartupUrl = IapRdpUrl.FromString("iap-rdp:///project-1/us-central1-a/vm-1")
            };

            Assert.That(
                options.ToString(), Is.EqualTo("/debug /url \"iap-rdp:///project-1/us-central1-a/vm-1?\" /profile \"some profile\""));
        }
    }
}
