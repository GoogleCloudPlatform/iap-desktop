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
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolConfigurationFile
    {
        //---------------------------------------------------------------------
        // ReadJson.
        //---------------------------------------------------------------------

        [Test]
        public void ReadJson_WhenNullOrEmptyOrMalformed(
            [Values(" ", "{,", "{}")] string json)
        {
            Assert.Throws<InvalidAppProtocolException>(
                () => AppProtocolConfigurationFile.ReadJson(json));
        }

        [Test]
        public void ReadJson_WhenJsonContainsNoVersion()
        {
            var json = @"
                {
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'remotePort': 8080
                }";

            Assert.Throws<InvalidAppProtocolException>(
                () => AppProtocolConfigurationFile.ReadJson(json));
        }

        [Test]
        public void ReadJson_WhenJsonContainsUnsupportedVersion()
        {
            var json = @"
                {
                    'version': 0,
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'remotePort': 8080
                }";

            Assert.Throws<InvalidAppProtocolException>(
                () => AppProtocolConfigurationFile.ReadJson(json));
        }

        [Test]
        public void ReadJson_WhenJsonIsValid()
        {
            var json = @"
                {
                    'version': 1,
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'remotePort': 8080,
                    'client': {
                        'executable': 'cmd'
                    }
                }";

            var protocol = AppProtocolConfigurationFile.ReadJson(json);
            Assert.That(protocol.Name, Is.EqualTo("protocol-1"));
            Assert.IsInstanceOf<WindowsTrait>(protocol.RequiredTraits.First());
            Assert.That(protocol.RemotePort, Is.EqualTo(8080));

            var client = (AppProtocolClient?)protocol.Client;
            Assert.That(client, Is.Not.Null);
            Assert.That(client!.Executable, Is.EqualTo("cmd"));
        }

        //---------------------------------------------------------------------
        // ReadStreamAsync.
        //---------------------------------------------------------------------

        [Test]
        public void ReadStreamAsync_WhenStreamDataEmptyOrMalformed(
            [Values("", " ", "{,", "{}")] string json)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, json);

            using (var stream = File.OpenRead(filePath))
            {
                ExceptionAssert.ThrowsAggregateException<InvalidAppProtocolException>(
                    () => AppProtocolConfigurationFile.ReadStreamAsync(stream).Wait());
            }
        }

        //---------------------------------------------------------------------
        // ReadFileAsync.
        //---------------------------------------------------------------------

        [Test]
        public async Task ReadStreamAsync_WhenFileNotFound()
        {
            await ExceptionAssert
                .ThrowsAsync<FileNotFoundException>(
                    () => AppProtocolConfigurationFile.ReadFileAsync("doesnotexist.json"))
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ReadStreamAsync_WhenFileEmptyOrMalformed(
            [Values("", " ", "{,", "{}")] string json)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, json);

            var e = await ExceptionAssert
                .ThrowsAsync<InvalidAppProtocolException>(
                    () => AppProtocolConfigurationFile.ReadFileAsync(filePath))
                .ConfigureAwait(false);

            Assert.That(e.Message, Does.Contain($"file {filePath}"));
        }
    }
}
