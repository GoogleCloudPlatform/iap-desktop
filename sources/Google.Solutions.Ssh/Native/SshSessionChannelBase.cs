using Google.Solutions.Common.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An interactive SSH channel.
    /// </summary>
    public abstract class SshSessionChannelBase : SshChannelBase
    {
        public const string Type = "session";

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshSessionChannelBase(SshChannelHandle channelHandle)
            : base(channelHandle)
        {
        }

        public int ExitCode
        {
            get
            {
                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    lock (this.channelHandle.SyncRoot)
                    {
                        // 
                        // NB. This call does not cause network traffic and therefore
                        // should not block.
                        //
                        return UnsafeNativeMethods.libssh2_channel_get_exit_status(
                            this.channelHandle);
                    }
                }
            }
        }

        public string ExitSignal
        {
            get
            {
                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    lock (this.channelHandle.SyncRoot)
                    {
                        // 
                        // NB. This call does not cause network traffic and therefore
                        // should not block.
                        //

                        var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_get_exit_signal(
                            this.channelHandle,
                            out IntPtr signalPtr,
                            out IntPtr signalLength,
                            out IntPtr errmsgPtr,
                            out IntPtr errmsgLength,
                            out IntPtr langTagPtr,
                            out IntPtr langTagLength);
                        if (result != LIBSSH2_ERROR.NONE)
                        {
                            throw new SshNativeException(result);
                        }

                        //
                        // NB. Currently, libssh2 only populates the signal
                        // parameter, errmsg and langtag are always NULL. 
                        //
                        try
                        {
                            if (signalPtr != IntPtr.Zero)
                            {
                                return Marshal.PtrToStringAnsi(signalPtr, signalLength.ToInt32());
                            }
                            else
                            {
                                return null;
                            }
                        }
                        finally
                        {
                            SshSession.FreeDelegate(signalPtr, IntPtr.Zero);
                            SshSession.FreeDelegate(errmsgPtr, IntPtr.Zero);
                            SshSession.FreeDelegate(langTagPtr, IntPtr.Zero);
                        }
                    }
                }
            }
        }
    }

    public class SshExecChannel : SshSessionChannelBase
    {
        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshExecChannel(SshChannelHandle channelHandle)
            : base(channelHandle)
        {
        }
    }

    public class SshShellChannel : SshSessionChannelBase
    {
        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshShellChannel(SshChannelHandle channelHandle)
            : base(channelHandle)
        {
        }

        public Task ResizePseudoTerminal(
            ushort widthInChars,
            ushort heightInChars)
        {
            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_request_pty_size_ex(
                        this.channelHandle,
                        widthInChars,
                        heightInChars,
                        0,
                        0);
                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                }
            });
        }
    }
}
