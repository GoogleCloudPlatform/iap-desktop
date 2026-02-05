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

using Google.Solutions.Mvvm.Diagnostics;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Diagnostics
{
    [TestFixture]
    public class TestMessageTraceRecorder
    {
        private void RecordMessage(MessageTraceRecorder recorder, int id)
        {
            var m = new System.Windows.Forms.Message()
            {
                Msg = id
            };
            recorder.Record(ref m);
        }

        //---------------------------------------------------------------------
        // History.
        //---------------------------------------------------------------------

        [Test]
        public void History_WhenBufferHasNotWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageTraceRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);

            var trace = recorder.Capture();
            Assert.That(trace.History.Count, Is.EqualTo(3));

            Assert.That(trace.History[0].Msg, Is.EqualTo(0));
            Assert.That(trace.History[1].Msg, Is.EqualTo(1));
            Assert.That(trace.History[2].Msg, Is.EqualTo(2));
        }

        [Test]
        public void History_WhenBufferHasWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageTraceRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);
            RecordMessage(recorder, 3);
            RecordMessage(recorder, 4);

            var trace = recorder.Capture();
            Assert.That(trace.History.Count, Is.EqualTo(3));

            Assert.That(trace.History[0].Msg, Is.EqualTo(2));
            Assert.That(trace.History[1].Msg, Is.EqualTo(3));
            Assert.That(trace.History[2].Msg, Is.EqualTo(4));
        }
    }
}
