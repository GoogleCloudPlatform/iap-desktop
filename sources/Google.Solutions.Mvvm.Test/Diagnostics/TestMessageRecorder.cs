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
using System;
using System.Linq;

namespace Google.Solutions.Mvvm.Test.Diagnostics
{
    [TestFixture]
    public class TestMessageRecorder
    {
        private void RecordMessage(MessageRecorder recorder, int id)
        {
            var m = new System.Windows.Forms.Message()
            {
                Msg = id
            };
            recorder.Record(ref m);
        }
        //---------------------------------------------------------------------
        // RecordedMessage.
        //---------------------------------------------------------------------

        [Test]
        public void RecordedMessageToString()
        {
            var m = new MessageRecorder.RecordedMessage()
            {
                Msg = 0x1,
                Lparam = new IntPtr(0x01234567),
                Wparam = new IntPtr(0x09ABCDEF)
            };

            Assert.AreEqual(
                "0x00000001 (LParam: 0x0000000001234567, WParam: 0x0000000009ABCDEF, WM_CREATE)",
                m.ToString());
        }

        //---------------------------------------------------------------------
        // History.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBufferHasNotWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);

            var history = recorder.History.ToList();
            Assert.AreEqual(3, history.Count());

            Assert.AreEqual(0, history[0].Msg);
            Assert.AreEqual(1, history[1].Msg);
            Assert.AreEqual(2, history[2].Msg);
        }

        [Test]
        public void WhenBufferHasWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);
            RecordMessage(recorder, 3);
            RecordMessage(recorder, 4);

            var history = recorder.History.ToList();
            Assert.AreEqual(3, history.Count());

            Assert.AreEqual(2, history[0].Msg);
            Assert.AreEqual(3, history[1].Msg);
            Assert.AreEqual(4, history[2].Msg);
        }
    }
}
