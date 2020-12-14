using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected and authenticated Libssh2 session.
    /// </summary>
    public class SshAuthenticatedSession : IDisposable
    {
        private readonly SshSessionHandle sessionHandle;
        private bool disposed = false;

        private readonly uint DefaultWindowSize = (2 * 1024 * 1024);
        private readonly uint DefaultPacketSize = 32768;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshAuthenticatedSession(SshSessionHandle sessionHandle)
        {
            this.sessionHandle = sessionHandle;
        }

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        public Task<SshSessionChannel> OpenSessionChannelAsync(
            string request,
            string command)
        {
            Utilities.ThrowIfNull(request, nameof(request));

            return Task.Run(() =>
            {
                lock (this.sessionHandle.SyncRoot)
                {
                    var channelHandle = UnsafeNativeMethods.libssh2_channel_open_ex(
                        this.sessionHandle,
                        SshSessionChannel.Type,
                        (uint)SshSessionChannel.Type.Length,
                        DefaultWindowSize,
                        DefaultPacketSize,
                        null,
                        0);

                    if (channelHandle.IsInvalid)
                    {
                        var lastError = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_last_errno(
                            this.sessionHandle);
                        if (lastError == LIBSSH2_ERROR.NONE)
                        {
                            throw new SshNativeException(LIBSSH2_ERROR.INVAL);
                        }
                        else
                        {
                            throw new SshNativeException(lastError);
                        }
                    }

                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_process_startup(
                        channelHandle,
                        request,
                        (uint)request.Length,
                        command,
                        command == null ? 0 : (uint)command.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        channelHandle.Dispose();
                        throw new SshNativeException(result);
                    }

                    return new SshSessionChannel(channelHandle);
                }
            });
        }

        public Task<SshSessionChannel> OpenShellChannelAsync()
            => OpenSessionChannelAsync("shell", null);

        public Task<SshSessionChannel> OpenExecChannelAsync(string command)
            => OpenSessionChannelAsync("exec", command);

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
                
            }
        }
    }
}
