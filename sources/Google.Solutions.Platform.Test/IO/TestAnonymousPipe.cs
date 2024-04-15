using Google.Solutions.Platform.IO;
using NUnit.Framework;
using System.Text;

namespace Google.Solutions.Platform.Test.IO
{
    [TestFixture]
    public class TestAnonymousPipe
    {
        //---------------------------------------------------------------------
        // Handle.
        //---------------------------------------------------------------------

        [Test]
        public void WhenWriteSideClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);

                pipe.CloseWriteSide();

                Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);
            }
        }

        [Test]
        public void WhenReadSideClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);

                pipe.CloseReadSide();

                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
            }
        }

        //---------------------------------------------------------------------
        // Stream.
        //---------------------------------------------------------------------

        [Test]
        public void WhenWriteStreamClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);

                pipe.WriteSide.Close();

                Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);
            }
        }

        [Test]
        public void WhenReadStreamClosed_ThenHandlesAreClosed()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsFalse(pipe.ReadSideHandle.IsClosed);

                pipe.ReadSide.Close();

                Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
                Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
            }
        }

        [Test]
        public void WhenDataWrittenToWriteSide_ThenDataCanBeReadFromReadSide()
        {
            using (var pipe = new AnonymousPipe())
            {
                Assert.IsTrue(pipe.ReadSide.CanRead);
                Assert.IsFalse(pipe.WriteSide.CanRead);

                Assert.IsFalse(pipe.ReadSide.CanWrite);
                Assert.IsTrue(pipe.WriteSide.CanWrite);

                var data = Encoding.ASCII.GetBytes("test");
                pipe.WriteSide.Write(data, 0, data.Length);
                pipe.WriteSide.Flush();

                var buffer = new byte[data.Length];
                var bytesRead = pipe.ReadSide.Read(buffer, 0, buffer.Length);

                Assert.AreEqual(data.Length, bytesRead);
                CollectionAssert.AreEqual(data, buffer);
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        [Test]
        public void Dispose()
        {
            var pipe = new AnonymousPipe();
            Assert.IsFalse(pipe.WriteSideHandle.IsClosed);
            Assert.IsFalse(pipe.ReadSideHandle.IsClosed);

            pipe.Dispose();

            Assert.IsTrue(pipe.WriteSideHandle.IsClosed);
            Assert.IsTrue(pipe.ReadSideHandle.IsClosed);
        }
    }
}
