using Google.Solutions.Common.Interop;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Interop
{
    [TestFixture]
    public class TestGlobalAllocSafeHandle : CommonFixtureBase
    {
        [Test]
        public void WhenFreed_ThenHandleIsInvalid()
        {
            var handle = GlobalAllocSafeHandle.GlobalAlloc(8);
            Assert.IsFalse(handle.IsClosed);
            Assert.IsFalse(handle.IsInvalid);

            handle.Dispose();

            Assert.IsTrue(handle.IsClosed);
            Assert.IsFalse(handle.IsInvalid);
        }
    }
}
