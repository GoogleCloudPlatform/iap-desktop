//
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
using NUnit.Framework;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    public class TestLocalShellClient
    {
        //----------------------------------------------------------------------
        // State.
        //----------------------------------------------------------------------

        [WindowsFormsTest]
        public void State_WhenLaunchingShellProgramFailed()
        {
            using (var form = new Form())
            {
                var client = new LocalShellClient("notfound.exe");
                form.Controls.Add(client);
                form.Show();

                Assert.AreEqual(ConnectionState.NotConnected, client.State);

                var connectionFailedEvents = 0;
                client.ConnectionFailed += (_, args) => connectionFailedEvents++;

                client.Connect();

                Assert.AreEqual(ConnectionState.NotConnected, client.State);
                Assert.AreEqual(1, connectionFailedEvents);
            }
        }
    }
}
