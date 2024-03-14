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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Testing.Application.Test;
using Google.Solutions.Testing.Application.Views;
using NUnit.Framework;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestSessionBroker : ApplicationFixtureBase
    {
        private static readonly InstanceLocator SampleLocator
            = new InstanceLocator("project-1", "zone-1", "instance-1");


        private class MockSession : DockContent, ISession
        {
            public bool IsConnected { get; set; } = true;

            public bool CanTransferFiles { get; set; } = false;

            public InstanceLocator Instance { get; set; }

            public bool IsClosing { get; set; } = false;

            public void ActivateSession()
            {
            }

            public Task DownloadFilesAsync()
            {
                throw new System.NotImplementedException();
            }

            public Task UploadFilesAsync()
            {
                throw new System.NotImplementedException();
            }
        }


        //---------------------------------------------------------------------
        // ActiveSession.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoSessionActive_ThenActiveSessionIsNull()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);
                Assert.IsNull(broker.ActiveSession);
            }
        }

        //---------------------------------------------------------------------
        // IsConnected.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoSessionActive_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);
                Assert.IsFalse(broker.IsConnected(SampleLocator));
            }
        }

        [Test]
        public void WhenOnlyOtherSessionsFound_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession()
                {
                    Instance = new InstanceLocator("project-1", "zone-1", "other-1")
                };
                new DockContent().Show(form.MainPanel, DockState.Document);
                session.Show(form.MainPanel, DockState.Document);

                Assert.IsFalse(broker.IsConnected(SampleLocator));
            }
        }

        [Test]
        public void WhenMatchingSessionFound_ThenIsConnectedReturnsTrue()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession()
                {
                    Instance = SampleLocator,
                    IsClosing = false
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.IsTrue(broker.IsConnected(SampleLocator));
            }
        }

        [Test]
        public void WhenMatchingSessionIsClosing_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession()
                {
                    Instance = SampleLocator,
                    IsClosing = true
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.IsFalse(broker.IsConnected(SampleLocator));
            }
        }

        //---------------------------------------------------------------------
        // TryActivate.
        //---------------------------------------------------------------------

        [Test]
        public void henNoSessionActive_ThenTryActivateReturnsFalse()
        {

            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);
                Assert.IsFalse(broker.TryActivateSession(SampleLocator, out var _));
            }
        }

        [Test]
        public void WhenMatchingSessionFound_ThenTryActivateReturnsTrue()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession()
                {
                    Instance = SampleLocator,
                    IsClosing = false
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.IsTrue(broker.TryActivateSession(
                    SampleLocator,
                    out var activated));
                Assert.AreSame(session, activated);
            }
        }

        [Test]
        public void WhenMatchingSessionIsClosing_ThenTryActivateReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession()
                {
                    Instance = SampleLocator,
                    IsClosing = true
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.IsFalse(broker.TryActivateSession(
                    SampleLocator,
                    out var activated));
                Assert.IsNull(activated);
            }
        }
    }
}
