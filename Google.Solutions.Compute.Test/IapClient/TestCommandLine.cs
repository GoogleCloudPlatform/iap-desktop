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

using NUnit.Framework;
using System;
using Google.Solutions.CloudIap.IapClient;

namespace Google.Solutions.Compute.Test.IapClient
{
    [TestFixture]
    public class TestCommandLine
    {
        [Test]
        public void MissingArgumentsCausesArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new string[0]));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "--project=p" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "--zone=z" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "--project=p", "--zone=z" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "--project=p", "ins:1" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "--zone=", "ins:1" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "ins:1", "" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "ins:", "" }));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "ins", "" }));
        }

        [Test]
        public void InvalidPortArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new string[0]));

            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "ins:70000", "--zone=z", "--project=p" }));
            Assert.Throws<ArgumentException>(() =>
                CommandLine.Parse(new[] { "ins:-1", "--zone=z", "--project=p" }));
        }

        [Test]
        public void ParseAllArguments()
        {
            var commandLine = CommandLine.Parse(new[] { "ins:10000", "--zone=z", "--project=p" });

            Assert.AreEqual("ins", commandLine.InstanceReference.InstanceName);
            Assert.AreEqual("z", commandLine.InstanceReference.Zone);
            Assert.AreEqual("p", commandLine.InstanceReference.ProjectId);
            Assert.AreEqual(10000, commandLine.Port);
        }
    }
}
