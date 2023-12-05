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
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
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
        // Enabled
        //---------------------------------------------------------------------

        [Test]
        public void WhenDisabled_ThenEventsAreNotCollected()
        {
            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                SynchronousQueueUserWorkItem);

            listener.Enabled = true;
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
        public void WhenEnabled_ThenEventsAreCollected()
        {
            var client = new Mock<IMeasurementClient>();
            var listener = new TelemetryCollector(
                client.Object,
                CreateInstall().Object,
                SynchronousQueueUserWorkItem);

            listener.Enabled = false;
            listener.Enabled = true;
            listener.Enabled = true;

            ApplicationEventSource.Log.CommandExecuted("test-command");

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
    }
}
