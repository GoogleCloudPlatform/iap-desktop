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

        public Task FlushAsync(int streamId)
        {
            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_flush_ex(
                        this.channelHandle,
                        streamId);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                }
            });
        }

        public Task FlushAsync() => FlushAsync(DefaultStream);
        public Task FlushStdErrAsync() => FlushAsync(StdErrStream);

        public Task<uint> ReadAsync(
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

        public Task<uint> ReadAsync(byte[] buffer) => ReadAsync(DefaultStream, buffer);

        public Task<uint> ReadStdErrAsync(byte[] buffer) => ReadAsync(StdErrStream, buffer);

        public Task<uint> WriteAsync(
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

        public Task<uint> WriteAsync(byte[] buffer) => WriteAsync(DefaultStream, buffer);

        public Task<uint> WriteStdErrAsync(byte[] buffer) => WriteAsync(StdErrStream, buffer);

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
