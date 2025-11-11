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
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Test
{
    [TestFixture]
    public class TestInitializeSessionExtension
    {
        //---------------------------------------------------------------------
        // LoadAndRegisterDefaultAppProtocols.
        //---------------------------------------------------------------------

        [Test]
        public async Task LoadAndRegisterDefaultAppProtocols()
        {
            var registry = new ProtocolRegistry();
            await InitializeSessionExtension
                .LoadAndRegisterDefaultAppProtocolsAsync(registry)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(registry.Protocols);
        }

        //---------------------------------------------------------------------
        // LoadAndRegisterCustomAppProtocols.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenDirectoryNotFound()
        {
            var registry = new ProtocolRegistry();
            await InitializeSessionExtension
                .LoadAndRegisterCustomAppProtocolsAsync("b:\\notfound", registry)
                .ConfigureAwait(false);

            CollectionAssert.IsEmpty(registry.Protocols);
        }

        [Test]
        public async Task WhenDirectoryContainsIapcFiles()
        {
            var dir = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            File.WriteAllText(
                $"{dir.FullName}\\sample.iapc",
                @"
                {
                    'version': 1,
                    'name': 'protocol-1',
                    'condition': 'isWindows()',
                    'remotePort': 8080,
                    'client': {
                        'executable': 'cmd'
                    }
                }");

            var registry = new ProtocolRegistry();
            await InitializeSessionExtension
                .LoadAndRegisterCustomAppProtocolsAsync(dir.FullName, registry)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(registry.Protocols);
        }
    }
}
