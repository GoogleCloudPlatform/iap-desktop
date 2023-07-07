﻿//
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
using System;
using System.IO;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Protocol
{
    [TestFixture]
    public class TestAppProtocolConfigurationFile
    {
        //---------------------------------------------------------------------
        // FromJson.
        //---------------------------------------------------------------------

        [Test]
        public void WhenJsonIsNullOrEmptyOrMalformed_ThenFromJsonThrowsException(
            [Values(null, " ", "{,", "{}")] string json)
        {
            Assert.Throws<InvalidAppProtocolException>(
                () => AppProtocolConfigurationFile.ReadJson(json));
        }

        [Test]
        public void WhenJsonContainsNoVersion_ThenFromJsonThrowsException()
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
        public void WhenJsonContainsUnsupportedVersion_ThenFromJsonThrowsException()
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
        public void WhenJsonIsValid_ThenFromJsonReturnsProtocol()
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
            Assert.AreEqual("protocol-1", protocol.Name);
            Assert.IsInstanceOf<WindowsTrait>(protocol.RequiredTraits.First());
            Assert.AreEqual(8080, protocol.RemotePort);

            var client = (AppProtocolClient)protocol.Client;
            Assert.AreEqual("cmd", client.Executable);
        }

        //---------------------------------------------------------------------
        // FromJson.
        //---------------------------------------------------------------------

        [Test]
        public void WhenFileNotFound_ThenFromFileThrowsException()
        {
            ExceptionAssert.ThrowsAggregateException<FileNotFoundException>(
                () => AppProtocolConfigurationFile.ReadFileAsync("doesnotexist.json").Wait());
            ExceptionAssert.ThrowsAggregateException<NotSupportedException>(
                () => AppProtocolConfigurationFile.ReadFileAsync("NUL.json").Wait());
        }

        [Test]
        public void WhenFileEmptyOrMalformed_ThenFromFileThrowsException(
            [Values("", " ", "{,", "{}")] string json)
        {
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, json);

            ExceptionAssert.ThrowsAggregateException<InvalidAppProtocolException>(
                $"file {filePath}",
                () => AppProtocolConfigurationFile.ReadFileAsync(filePath).Wait());
        }
    }
}
