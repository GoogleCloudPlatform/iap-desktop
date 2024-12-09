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
    public class TestAnalyticsLog
    {
        private static Mock<IInstall> CreateInstall()
        {
            var install = new Mock<IInstall>();
            install
                .SetupGet(i => i.UniqueId)
                .Returns("unique-id");

            return install;
        }

        //---------------------------------------------------------------------
        // Write.
        //---------------------------------------------------------------------

        [Test]
        public void Write_WhenNotEnbled()
        {
            var client = new Mock<IMeasurementClient>();
            var install = CreateInstall();
            var log = new AnalyticsLog(
                client.Object,
                install.Object,
                new Dictionary<string, object>(),
                false);

            log.Write(
                "event-name",
                new Dictionary<string, object>());

            client.Verify(
                c => c.CollectEventAsync(
                    It.IsAny<MeasurementSession>(),
                    "event-name",
                    It.IsAny<IEnumerable<KeyValuePair<string, string>>>(),
                    CancellationToken.None),
                Times.Never);
        }

        [Test]
        public void Write_AddsDefaultParameters()
        {
            var defaultParameters = new Dictionary<string, object>
            {
                { "default-1", 123 },
            };

            var client = new Mock<IMeasurementClient>();
            var install = CreateInstall();
            var log = new AnalyticsLog(
                client.Object,
                install.Object,
                defaultParameters,
                false)
            {
                Enabled = true
            };

            log.Write(
                "event-name",
                new Dictionary<string, object>
                {
                    { "parameter-1", "1" },
                    { "parameter-2", 123 },
                });

            client.Verify(
                c => c.CollectEventAsync(
                    It.Is<MeasurementSession>(s => s.ClientId == "unique-id"),
                    "event-name",
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(p => p
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        ["default-1"] == "123"),
                    CancellationToken.None),
                Times.Once);
        }

        [Test]
        public void Write_AddsParameters()
        {
            var client = new Mock<IMeasurementClient>();
            var install = CreateInstall();
            var log = new AnalyticsLog(
                client.Object,
                install.Object,
                new Dictionary<string, object>(),
                false)
            {
                Enabled = true
            };

            log.Write(
                "event-name",
                new Dictionary<string, object>
                {
                    { "parameter-1", "1" },
                });

            client.Verify(
                c => c.CollectEventAsync(
                    It.Is<MeasurementSession>(s => s.ClientId == "unique-id"),
                    "event-name",
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(p => p
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        ["parameter-1"] == "1"),
                    CancellationToken.None),
                Times.Once);
        }

        [Test]
        public void Write_WhenParameterNamesCollide()
        {
            var defaultParameters = new Dictionary<string, object>
            {
                { "default-1", 123 },
            };

            var client = new Mock<IMeasurementClient>();
            var install = CreateInstall();
            var log = new AnalyticsLog(
                client.Object,
                install.Object,
                defaultParameters,
                false)
            {
                Enabled = true
            };

            log.Write(
                "event-name",
                new Dictionary<string, object>
                {
                    { "default-1", 456 },
                });
            client.Verify(
                c => c.CollectEventAsync(
                    It.Is<MeasurementSession>(s => s.ClientId == "unique-id"),
                    "event-name",
                    It.Is<IEnumerable<KeyValuePair<string, string>>>(p => p.Count() == 2),
                    CancellationToken.None),
                Times.Once);
        }
    }
}
