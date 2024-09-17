//
// Copyright 2022 Google LLC
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

using Google.Solutions.Apis.Analytics;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Google.Solutions.IapDesktop.Application.Test.Diagnostics
{
    [TestFixture]
    public class TestTelemetryListener
    {
        private static bool SynchronousQueueUserWorkItem(WaitCallback waitCallback)
        {
            waitCallback(null);
            return true;
        }

        private static Mock<IInstall> CreateInstall()
        {
            var install = new Mock<IInstall>();
            install.SetupGet(i => i.UniqueId).Returns("unique-id");
            return install;
        }

        //---------------------------------------------------------------------
        // CollectEvent - enable/disable.
        //---------------------------------------------------------------------

        [Test]
        public void CollectEvent_WhenDisabled_ThenEventsAreNotCollected()
        {
            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                new Dictionary<string, string>(),
                SynchronousQueueUserWorkItem)
            {
                Enabled = true
            };

            listener.Enabled = false;
            listener.Enabled = false;

            ApplicationEventSource.Log.CommandExecuted("test-command");

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void CollectEvent_WhenEnabled_ThenEventsAreCollected()
        {
            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                new Dictionary<string, string>(),
                SynchronousQueueUserWorkItem)
            {
                Enabled = false
            };

            listener.Enabled = true;
            listener.Enabled = true;

            ApplicationEventSource.Log.CommandExecuted("test-command");

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        //---------------------------------------------------------------------
        // CollectEvent.
        //---------------------------------------------------------------------

        [Test]
        public void CollectEvent_IncludesDefaultParameters()
        {

            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                new Dictionary<string, string>()
                {
                    { "default-1", "1" },
                    { "default-2", "2" }
                },
                SynchronousQueueUserWorkItem)
            {
                Enabled = true
            };

            ApplicationEventSource.Log.CommandExecuted("test-command");

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    It.IsAny<string>(),
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(p => p
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        .ContainsKey("default-1")),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void CollectEvent_IncludesEventParameters()
        {

            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                new Dictionary<string, string>(),
                SynchronousQueueUserWorkItem)
            {
                Enabled = true
            };

            ApplicationEventSource.Log.CommandExecuted("test-command");

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    It.IsAny<string>(),
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(p => p
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        .ContainsKey("id")),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
    }
}
