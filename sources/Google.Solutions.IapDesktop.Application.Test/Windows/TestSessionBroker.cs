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
            public MockSession(InstanceLocator instance)
            {
                this.Instance = instance;
            }

            public bool IsConnected { get; set; } = true;

            public bool CanTransferFiles { get; set; } = false;

            public InstanceLocator Instance { get; }

            public bool IsClosing { get; set; } = false;

            public void ActivateSession()
            {
            }

            public Task TransferFilesAsync()
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
                Assert.That(broker.ActiveSession, Is.Null);
            }
        }

        //---------------------------------------------------------------------
        // IsConnected.
        //---------------------------------------------------------------------

        [Test]
        public void IsConnected_WhenNoSessionActive_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);
                Assert.That(broker.IsConnected(SampleLocator), Is.False);
            }
        }

        [Test]
        public void IsConnected_WhenOnlyOtherSessionsFound_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession(
                    new InstanceLocator("project-1", "zone-1", "other-1"));
                new DockContent().Show(form.MainPanel, DockState.Document);
                session.Show(form.MainPanel, DockState.Document);

                Assert.That(broker.IsConnected(SampleLocator), Is.False);
            }
        }

        [Test]
        public void IsConnected_WhenMatchingSessionFound_ThenIsConnectedReturnsTrue()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession(SampleLocator)
                {
                    IsClosing = false
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.That(broker.IsConnected(SampleLocator), Is.True);
            }
        }

        [Test]
        public void IsConnected_WhenMatchingSessionIsClosing_ThenIsConnectedReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession(SampleLocator)
                {
                    IsClosing = true
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.That(broker.IsConnected(SampleLocator), Is.False);
            }
        }

        //---------------------------------------------------------------------
        // TryActivateSession.
        //---------------------------------------------------------------------

        [Test]
        public void TryActivateSession_WhenNoSessionActive_ThenTryActivateReturnsFalse()
        {

            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);
                Assert.That(broker.TryActivateSession(SampleLocator, out var _), Is.False);
            }
        }

        [Test]
        public void TryActivateSession_WhenMatchingSessionFound_ThenTryActivateReturnsTrue()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession(SampleLocator)
                {
                    IsClosing = false
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.That(broker.TryActivateSession(
                    SampleLocator,
                    out var activated), Is.True);
                Assert.That(activated, Is.SameAs(session));
            }
        }

        [Test]
        public void TryActivateSession_WhenMatchingSessionIsClosing_ThenTryActivateReturnsFalse()
        {
            using (var form = new TestMainForm())
            {
                var broker = new SessionBroker(form);

                var session = new MockSession(SampleLocator)
                {
                    IsClosing = true
                };
                session.Show(form.MainPanel, DockState.Document);

                Assert.That(broker.TryActivateSession(
                    SampleLocator,
                    out var activated), Is.False);
                Assert.That(activated, Is.Null);
            }
        }
    }
}
