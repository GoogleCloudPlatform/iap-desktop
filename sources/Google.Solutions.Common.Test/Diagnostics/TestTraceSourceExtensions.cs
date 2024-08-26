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

using Google.Solutions.Common.Diagnostics;
using Moq;
using NUnit.Framework;
using System.Diagnostics;

namespace Google.Solutions.Common.Test.Diagnostics
{
    [TestFixture]
    public class TestTraceSourceExtensions
    {
        [Test]
        public void TraceEvent_WhenLevelNotEnabled_ThenCallsAreNotForwarded()
        {
            var listener = new Mock<TraceListener>();
            var destination = new TraceSource("destination");
            destination.Switch.Level = SourceLevels.Critical;
            destination.Listeners.Add(listener.Object);

            var source = new TraceSource("source");
            source.ForwardTo(destination);

            source.TraceEvent(TraceEventType.Error, 1, "message");
            listener.Verify(l => l.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                It.IsAny<TraceEventType>(),
                It.IsAny<int>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TraceEvent_WhenLevelEnabled_ThenCallsAreForwarded()
        {
            var listener = new Mock<TraceListener>();
            var destination = new TraceSource("destination");
            destination.Switch.Level = SourceLevels.All;
            destination.Listeners.Add(listener.Object);

            var source = new TraceSource("source");
            source.ForwardTo(destination);

            source.TraceEvent(TraceEventType.Error, 1, "message");
            listener.Verify(l => l.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                It.Is<TraceEventType>(t => t == TraceEventType.Error),
                It.Is<int>(id => id == 1),
                It.Is<string>(m => m == "message")), Times.Once);
        }

        [Test]
        public void TraceEvent_WhenLevelEnabled_ThenCallsWithArgsAreForwarded()
        {
            var listener = new Mock<TraceListener>();
            var destination = new TraceSource("destination");
            destination.Switch.Level = SourceLevels.All;
            destination.Listeners.Add(listener.Object);

            var source = new TraceSource("source");
            source.ForwardTo(destination);

            source.TraceEvent(TraceEventType.Error, 1, "message {0}", 0);
            listener.Verify(l => l.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                It.Is<TraceEventType>(t => t == TraceEventType.Error),
                It.Is<int>(id => id == 1),
                It.Is<string>(m => m == "message {0}"),
                It.IsAny<object[]>()), Times.Once);
        }

        [Test]
        public void TraceEvent_WhenLevelEnabled_TraceInformationCallsAreForwarded()
        {
            var listener = new Mock<TraceListener>();
            var destination = new TraceSource("destination");
            destination.Switch.Level = SourceLevels.All;
            destination.Listeners.Add(listener.Object);

            var source = new TraceSource("source");
            source.ForwardTo(destination);

            source.TraceInformation("message");
            listener.Verify(l => l.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                It.Is<TraceEventType>(t => t == TraceEventType.Information),
                It.Is<int>(id => id == 0),
                It.Is<string>(m => m == "message"),
                It.IsAny<object[]>()), Times.Once);
        }

        [Test]
        public void TraceEvent_WhenLevelEnabled_TraceInformationCallsWithArgsAreForwarded()
        {
            var listener = new Mock<TraceListener>();
            var destination = new TraceSource("destination");
            destination.Switch.Level = SourceLevels.All;
            destination.Listeners.Add(listener.Object);

            var source = new TraceSource("source");
            source.ForwardTo(destination);

            source.TraceInformation("message {0}", 0);
            listener.Verify(l => l.TraceEvent(
                It.IsAny<TraceEventCache>(),
                It.IsAny<string>(),
                It.Is<TraceEventType>(t => t == TraceEventType.Information),
                It.Is<int>(id => id == 0),
                It.Is<string>(m => m == "message {0}"),
                It.IsAny<object[]>()), Times.Once);
        }
    }
}
