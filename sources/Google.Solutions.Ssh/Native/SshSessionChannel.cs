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
    /// An interactive SSH channel.
    /// </summary>
    public class SshSessionChannel : SshChannelBase
    {
        public const string Type = "session";

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshSessionChannel(SshChannelHandle channelHandle)
            : base(channelHandle)
        {
        }

        //---------------------------------------------------------------------
        // Environment.
        //---------------------------------------------------------------------

        public Task SetEnvironmentVariable(
            string variableName,
            string value)
        {
            Utilities.ThrowIfNullOrEmpty(variableName, nameof(variableName));
            Utilities.ThrowIfNullOrEmpty(value, nameof(value));

            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_setenv_ex(
                        this.channelHandle,
                        variableName,
                        (uint)variableName.Length,
                        value,
                        (uint)value.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                }
            });
        }

        //---------------------------------------------------------------------
        // Process/Shell.
        //---------------------------------------------------------------------

        public Task StartShell()
        {
            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var request = "shell";
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_process_startup(
                        this.channelHandle,
                        request,
                        (uint)request.Length,
                        null,
                        0);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                }
            });
        }

        public Task Execute(string command)
        {
            return Task.Run(() =>
            {
                lock (this.channelHandle.SyncRoot)
                {
                    var request = "exec";
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_process_startup(
                        this.channelHandle,
                        request,
                        (uint)request.Length,
                        command,
                        (uint)command.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                }
            });
        }

        public int ExitCode
        {
            get
            {
                lock (this.channelHandle.SyncRoot)
                {
                    return UnsafeNativeMethods.libssh2_channel_get_exit_status(
                        this.channelHandle);
                }
            }
        }

        public string ExitSignal
        {
            get
            {
                lock (this.channelHandle.SyncRoot)
                {
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
