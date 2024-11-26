﻿//
// Copyright 2024 Google LLC
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

using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    [RequiresInteraction]
    [Apartment(ApartmentState.STA)]
    public class TestSshHybridClient : SshClientFixtureBase
    {
        //---------------------------------------------------------------------
        // IsFileBrowserVisible.
        //---------------------------------------------------------------------

        [WindowsFormsTest]
        public async Task IsFileBrowserVisible_WhenFileBrowserEnabled()
        {
            using (var window = CreateWindow<SshHybridClient>())
            {
                window.Show();

                Assert.IsTrue(window.Client.EnableFileBrowser);
                Assert.IsFalse(window.Client.IsFileBrowserVisible);
                Assert.IsFalse(window.Client.CanShowFileBrowser);

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                Assert.IsFalse(window.Client.IsFileBrowserVisible);
                Assert.IsTrue(window.Client.CanShowFileBrowser);

                window.Client.IsFileBrowserVisible = true;
                window.Client.IsFileBrowserVisible = true;

                await Task.Delay(TimeSpan.FromSeconds(2));

                window.Client.IsFileBrowserVisible = false;
                await Task.Delay(TimeSpan.FromSeconds(2));

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(ConnectionState.NotConnected)
                    .ConfigureAwait(true);
            }
        }

        [WindowsFormsTest]
        public async Task IsFileBrowserVisible_WhenFileBrowserDisabled()
        {
            using (var window = CreateWindow<SshHybridClient>())
            {
                window.Show();

                window.Client.EnableFileBrowser = false;

                Assert.IsFalse(window.Client.IsFileBrowserVisible);
                Assert.IsFalse(window.Client.CanShowFileBrowser);

                //
                // Connect.
                //
                window.Client.Connect();
                await window.Client
                    .AwaitStateAsync(ConnectionState.LoggedOn)
                    .ConfigureAwait(true);

                Assert.IsFalse(window.Client.IsFileBrowserVisible);
                Assert.IsFalse(window.Client.CanShowFileBrowser);

                //
                // Close window.
                //
                window.Close();

                await window.Client
                    .AwaitStateAsync(ConnectionState.NotConnected)
                    .ConfigureAwait(true);
            }
        }
    }
}
