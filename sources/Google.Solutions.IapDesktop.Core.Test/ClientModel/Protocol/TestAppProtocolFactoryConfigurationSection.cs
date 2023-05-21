//
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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolFactoryConfigurationSection
    {
        //---------------------------------------------------------------------
        // ParseName.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNullOrEmpty_ThenParseNameThrowsException(
            [Values(" ", "", null)] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Name = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseName());
        }

        //---------------------------------------------------------------------
        // ParseCondition.
        //---------------------------------------------------------------------

        [Test]
        public void WhenConditionIsNullOrEmpty_ThenParseConditionReturnsEmpty(
            [Values(" ", "", null)] string condition)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Condition = condition
            };

            CollectionAssert.IsEmpty(section.ParseCondition());
        }

        [Test]
        public void WhenConditionContainsSingleClause_ThenParseConditionReturnsTraits(
            [Values("isInstance()", " \nisInstance( )\r\n")] string condition)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Condition = condition
            };

            var traits = section.ParseCondition();
            CollectionAssert.IsNotEmpty(traits);

            Assert.IsTrue(traits.All(t => t is InstanceTrait));
        }

        [Test]
        public void WhenConditionContainsMultipleClauses_ThenParseConditionReturnsTraits()
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Condition = "isInstance() && isWindows() &&isLinux() "
            };

            var traits = section.ParseCondition();
            CollectionAssert.IsNotEmpty(traits);

            Assert.AreEqual(3, traits.Count());
            Assert.IsTrue(traits.Any(t => t is InstanceTrait));
            Assert.IsTrue(traits.Any(t => t is WindowsTrait));
            Assert.IsTrue(traits.Any(t => t is LinuxTrait));
        }

        [Test]
        public void WhenConditionContainsUnknownClause_ThenParseConditionThrowsException(
            [Values("isFoo()", " \nisInstance( ) && isBar\r\n")] string condition)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Condition = condition
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseCondition().ToList());
        }

        //---------------------------------------------------------------------
        // ParseAccessPolicy.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNullOrEmptyOrUnrecognized_ThenParseAccessPolicyThrowsException(
            [Values(" ", "", null, "allowall()", "DenyAll")] string policy)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                AccessPolicy = policy
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseAccessPolicy());
        }

        [Test]
        public void WhenValueIsValid_ThenParseAccessPolicyreturnsPolicy(
            [Values(" allowAll\t")] string policy)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                AccessPolicy = policy
            };

            Assert.IsNotNull(section.ParseAccessPolicy());
            Assert.IsInstanceOf<AllowAllPolicy>(section.ParseAccessPolicy());
        }

        //---------------------------------------------------------------------
        // ParseRemotePort.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNullOrEmpty_ThenParseRemotePortThrowsException(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                RemotePort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseRemotePort());
        }


        [Test]
        public void WhenValueIsMalformed_ThenParseRemotePortThrowsException(
            [Values("test", "-1", "100000")] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                RemotePort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseRemotePort());
        }

        [Test]
        public void WhenValueIsValid_ThenParseRemotePortReturnsPort()
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                RemotePort = "80"
            };

            Assert.AreEqual(80, section.ParseRemotePort());
        }

        //---------------------------------------------------------------------
        // ParseLocalEndpoint.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueIsNullOrEmpty_ThenParseLocalEndpointReturnsNull(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                LocalPort = value
            };

            Assert.IsNull(section.ParseLocalEndpoint());
        }

        [Test]
        public void WhenValueIsMalformed_ThenParseLocalEndpointThrowsException(
            [Values("::", "test:0", "127.0.0.1:test", ":")] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                LocalPort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseLocalEndpoint());
        }

        [Test]
        public void WhenValueIsValid_ThenParseLocalEndpointReturnsEndpoint()
        {
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Loopback, 80),
                new AppProtocolFactory.ConfigurationSection()
                {
                    LocalPort = "80"
                }.ParseLocalEndpoint());
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Loopback, 80),
                new AppProtocolFactory.ConfigurationSection()
                {
                    LocalPort = "127.0.0.1:80"
                }.ParseLocalEndpoint());
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Parse("127.0.0.2"), 80),
                new AppProtocolFactory.ConfigurationSection()
                {
                    LocalPort = "127.0.0.2:80"
                }.ParseLocalEndpoint());
        }

        //---------------------------------------------------------------------
        // ParseCommand.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCommandIsNull_ThenParseCommandReturnsEmpty()
        {
            Assert.IsNull(new AppProtocolFactory.ConfigurationSection().ParseCommand());
        }

        [Test]
        public void WhenCommandExecutableIsNullOrEmpty_ThenParseCommandReturnsEmpty(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Command = new AppProtocolFactory.CommandSection()
                {
                    Executable = value
                }
            };

            Assert.IsNull(section.ParseCommand());
        }

        [Test]
        public void WhenCommandContainsVariables_ThenParseCommandExpandsVariables()
        {
            var section = new AppProtocolFactory.ConfigurationSection()
            {
                Command = new AppProtocolFactory.CommandSection()
                {
                    Executable = "%ProgramFiles(x86)%\\foo.exe",
                    Arguments = "%ProgramFiles(x86)%\\foo.txt %host%",
                }
            };

            var command = section.ParseCommand();
            Assert.IsNotNull(command);

            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            StringAssert.Contains(programsFolder, command.Executable);
            StringAssert.Contains(programsFolder, command.Arguments);
            StringAssert.Contains("%host%", command.Arguments);
        }
    }
}
