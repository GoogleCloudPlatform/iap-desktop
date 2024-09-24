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
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolConfigurationFileSection
    {
        //---------------------------------------------------------------------
        // ParseName.
        //---------------------------------------------------------------------

        [Test]
        public void ParseName_WhenValueIsNullOrEmpty(
            [Values(" ", "", null)] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Name = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseName());
        }

        //---------------------------------------------------------------------
        // ParseCondition.
        //---------------------------------------------------------------------

        [Test]
        public void ParseCondition_WhenConditionIsNullOrEmpty(
            [Values(" ", "", null)] string condition)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Condition = condition
            };

            CollectionAssert.IsEmpty(section.ParseCondition());
        }

        [Test]
        public void ParseCondition_WhenConditionContainsSingleClause(
            [Values("isInstance()", " \nisInstance( )\r\n")] string condition)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Condition = condition
            };

            var traits = section.ParseCondition();
            CollectionAssert.IsNotEmpty(traits);

            Assert.IsTrue(traits.All(t => t is InstanceTrait));
        }

        [Test]
        public void ParseCondition_WhenConditionContainsMultipleClauses()
        {
            var section = new AppProtocolConfigurationFile.MainSection()
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
        public void ParseCondition_WhenConditionContainsUnknownClause(
            [Values("isFoo()", " \nisInstance( ) && isBar\r\n")] string condition)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Condition = condition
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseCondition().ToList());
        }

        //---------------------------------------------------------------------
        // ParseRemotePort.
        //---------------------------------------------------------------------

        [Test]
        public void ParseRemotePort_WhenValueIsNullOrEmpty(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                RemotePort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseRemotePort());
        }


        [Test]
        public void ParseRemotePort_WhenValueIsMalformed(
            [Values("test", "-1", "100000")] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                RemotePort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseRemotePort());
        }

        [Test]
        public void ParseRemotePort_WhenValueIsValid()
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                RemotePort = "80"
            };

            Assert.AreEqual(80, section.ParseRemotePort());
        }

        //---------------------------------------------------------------------
        // ParseLocalEndpoint.
        //---------------------------------------------------------------------

        [Test]
        public void ParseLocalEndpoint_WhenValueIsNullOrEmpty(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                LocalPort = value
            };

            Assert.IsNull(section.ParseLocalEndpoint());
        }

        [Test]
        public void ParseLocalEndpoint_WhenValueIsMalformed(
            [Values("::", "test:0", "127.0.0.1:test", ":")] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                LocalPort = value
            };

            Assert.Throws<InvalidAppProtocolException>(() => section.ParseLocalEndpoint());
        }

        [Test]
        public void ParseLocalEndpoint_WhenValueIsValid()
        {
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Loopback, 80),
                new AppProtocolConfigurationFile.MainSection()
                {
                    LocalPort = "80"
                }.ParseLocalEndpoint());
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Loopback, 80),
                new AppProtocolConfigurationFile.MainSection()
                {
                    LocalPort = "127.0.0.1:80"
                }.ParseLocalEndpoint());
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Parse("127.0.0.2"), 80),
                new AppProtocolConfigurationFile.MainSection()
                {
                    LocalPort = "127.0.0.2:80"
                }.ParseLocalEndpoint());
        }

        //---------------------------------------------------------------------
        // ParseClientSection.
        //---------------------------------------------------------------------

        [Test]
        public void ParseClientSection_WhenClientIsNull()
        {
            Assert.IsNull(new AppProtocolConfigurationFile.MainSection().ParseClientSection());
        }

        [Test]
        public void ParseClientSection_WhenClientExecutableIsNullOrEmpty(
            [Values(null, "", " \n")] string value)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Client = new AppProtocolConfigurationFile.ClientSection()
                {
                    Executable = value
                }
            };

            Assert.IsNull(section.ParseClientSection());
        }

        [Test]
        public void ParseClientSection_WhenClientExecutableContainsVariables()
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Client = new AppProtocolConfigurationFile.ClientSection()
                {
                    Executable = "%ProgramFiles(x86)%\\foo.exe",
                    Arguments = "%ProgramFiles(x86)%\\foo.txt {host}",
                }
            };

            var client = (AppProtocolClient?)section.ParseClientSection();
            Assert.IsNotNull(client);

            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            StringAssert.Contains(programsFolder, client!.Executable);
            StringAssert.Contains(programsFolder, client.ArgumentsTemplate);
            StringAssert.Contains("{host}", client.ArgumentsTemplate);
        }

        [Test]
        public void ParseClientSection_WhenClientExecutableContainsFilenameOfRegisteredApp()
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Client = new AppProtocolConfigurationFile.ClientSection()
                {
                    Executable = "powershell.exe"
                }
            };

            var client = (AppProtocolClient?)section.ParseClientSection();
            Assert.IsNotNull(client);
            Assert.IsTrue(File.Exists(client!.Executable));
        }

        [Test]
        public void ParseClientSection_WhenClientExecutableContainsFilename(
            [Values("notaregisteredapp.exe", "notanapp.txt")] string fileName)
        {
            var section = new AppProtocolConfigurationFile.MainSection()
            {
                Client = new AppProtocolConfigurationFile.ClientSection()
                {
                    Executable = fileName
                }
            };

            var client = (AppProtocolClient?)section.ParseClientSection();
            Assert.IsNotNull(client);
            Assert.AreEqual(fileName, client!.Executable);
        }
    }
}
