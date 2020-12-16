using Google.Solutions.Ssh.Native;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Google.Solutions.Ssh.Test
{
    public abstract class SshFixtureBase
    {
        private static readonly ConsoleTraceListener listener = new ConsoleTraceListener();

        private static readonly TraceSource[] Traces = new[]
        {
            Google.Solutions.Common.TraceSources.Common,
            SshTraceSources.Default,
        };

        //---------------------------------------------------------------------
        // Tracing.
        //---------------------------------------------------------------------

        [SetUp]
        public void SetUpTracing()
        {
            foreach (var trace in Traces)
            {
                if (!trace.Listeners.Contains(listener))
                {
                    listener.TraceOutputOptions = TraceOptions.DateTime;
                    trace.Listeners.Add(listener);
                    trace.Switch.Level = System.Diagnostics.SourceLevels.Verbose;
                }
            }

            listener.WriteLine("Start " + TestContext.CurrentContext.Test.FullName);
        }

        [TearDown]
        public void TearDownTracing()
        {
            listener.WriteLine("End " + TestContext.CurrentContext.Test.FullName);
        }

        //---------------------------------------------------------------------
        // Handle tracking.
        //---------------------------------------------------------------------

        [SetUp]
        public void ClearOpenHandles()
        {
            HandleTable.Clear();
        }

        [TearDown]
        public void CheckOpenHandles()
        {
            HandleTable.DumpOpenHandles();
            Assert.AreEqual(0, HandleTable.HandleCount);
        }

        //---------------------------------------------------------------------
        // Helper methods.
        //---------------------------------------------------------------------

        protected static SshSession CreateSession()
        {
            var session = new SshSession();
            session.SetTraceHandler(
                LIBSSH2_TRACE.SOCKET | LIBSSH2_TRACE.ERROR | LIBSSH2_TRACE.CONN |
                                       LIBSSH2_TRACE.AUTH | LIBSSH2_TRACE.KEX,
                Console.WriteLine);

            session.Timeout = TimeSpan.FromSeconds(5);
            return session;
        }

    }
}
