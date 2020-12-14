using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public abstract class SshChannelBase : IDisposable
    {
        internal readonly SshChannelHandle channelHandle;
        private bool disposed = false;

        private const int DefaultStream = 0;
        private const int StdErrStream = 1;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshChannelBase(SshChannelHandle channelHandle)
        {
            this.channelHandle = channelHandle;
        }

        //---------------------------------------------------------------------
        // I/O.
        //---------------------------------------------------------------------

        public Task<uint> Read(
            int streamId,
            byte[] buffer)
        {
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var bytesRead = UnsafeNativeMethods.libssh2_channel_read_ex(
                        this.channelHandle,
                        streamId,
                        buffer,
                        new IntPtr(buffer.Length));

                    if (bytesRead < 0)
                    {
                        throw new SshNativeException((LIBSSH2_ERROR)bytesRead);
                    }
                    else
                    {
                        return (uint)bytesRead;
                    }
                }
            });
        }

        public Task<uint> Read(byte[] buffer) => Read(DefaultStream, buffer);

        public Task<uint> ReadStdErr(byte[] buffer) => Read(StdErrStream, buffer);

        public Task<uint> Write(
            int streamId,
            byte[] buffer)
        {
            Utilities.ThrowIfNull(buffer, nameof(buffer));

            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var bytesWritten = UnsafeNativeMethods.libssh2_channel_write_ex(
                        this.channelHandle,
                        streamId,
                        buffer,
                        new IntPtr(buffer.Length));

                    if (bytesWritten < 0)
                    {
                        throw new SshNativeException((LIBSSH2_ERROR)bytesWritten);
                    }
                    else
                    {
                        return (uint)bytesWritten;
                    }
                }
            });
        }

        public Task<uint> Write(byte[] buffer) => Write(DefaultStream, buffer);

        public Task<uint> WriteStdErr(byte[] buffer) => Write(StdErrStream, buffer);

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
