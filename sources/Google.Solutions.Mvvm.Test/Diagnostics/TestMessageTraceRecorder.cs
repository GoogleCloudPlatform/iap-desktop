using Google.Solutions.Mvvm.Diagnostics;
using NUnit.Framework;
using System;
using System.Windows.Forms;

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
        public void WhenBufferHasNotWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageTraceRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);

            var trace = recorder.Capture();
            Assert.AreEqual(3, trace.History.Count);

            Assert.AreEqual(0, trace.History[0].Msg);
            Assert.AreEqual(1, trace.History[1].Msg);
            Assert.AreEqual(2, trace.History[2].Msg);
        }

        [Test]
        public void WhenBufferHasWrappedAround_ThenHistoryStartsWithEmptyEntries()
        {
            var recorder = new MessageTraceRecorder(3);
            RecordMessage(recorder, 1);
            RecordMessage(recorder, 2);
            RecordMessage(recorder, 3);
            RecordMessage(recorder, 4);

            var trace = recorder.Capture();
            Assert.AreEqual(3, trace.History.Count);

            Assert.AreEqual(2, trace.History[0].Msg);
            Assert.AreEqual(3, trace.History[1].Msg);
            Assert.AreEqual(4, trace.History[2].Msg);
        }
    }
}
