using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments

namespace Google.Solutions.Ssh.Native
{
    public enum LIBSSH2_HOSTKEY_HASH : Int32
    {
        MD5 = 1,
        SHA1 = 2,
        SHA256 = 3
    }

    public enum LIBSSH2_METHOD : Int32
    {
        KEX       = 0,
        HOSTKEY   = 1,
        CRYPT_CS  = 2,  // Client -> Server
        CRYPT_SC  = 3,  // Server -> Client
        MAC_CS    = 4,  // Client -> Server
        MAC_SC    = 5,  // Server -> Client
        COMP_CS   = 6,  // Compression Client -> Server
        COMP_SC   = 7,  // Compression Server -> Client
        LANG_CS   = 8,  // Client -> Server
        LANG_SC   = 9,  // Server -> Client
    }

    [Flags]
    public enum LIBSSH2_TRACE : Int32
    {
        TRANS     = (1<<1),
        KEX       = (1<<2),
        AUTH      = (1<<3),
        CONN      = (1<<4),
        SCP       = (1<<5),
        SFTP      = (1<<6),
        ERROR     = (1<<7),
        PUBLICKEY = (1<<8),
        SOCKET    = (1<<9)
    }

    internal static class UnsafeNativeMethods
    {
        private const string Libssh2 = "libssh2.dll";

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_init(Int32 flags);

        
        //---------------------------------------------------------------------
        // Session functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern SshSessionHandle libssh2_session_init_ex(
            IntPtr alloc,
            IntPtr free,
            IntPtr realloc,
            IntPtr userData);

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_free(
            SshSessionHandle session,
            IntPtr ptr);

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_session_free(
            IntPtr session);

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern Int32 libssh2_session_disconnect_ex(
            SshSessionHandle session,
            Int32 reason,
            [MarshalAs(UnmanagedType.LPStr)] string description,
            [MarshalAs(UnmanagedType.LPStr)] string lang);

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_session_get_blocking(
            SshSessionHandle session);

        [DllImport(Libssh2)]
        public static extern void libssh2_session_set_blocking(
            SshSessionHandle session,
            Int32 blocking);

        //---------------------------------------------------------------------
        // Algorithm functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern IntPtr libssh2_session_methods(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType);

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_session_supported_algs(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType,
            [Out] out IntPtr algorithmsPtrPtr);

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern Int32 libssh2_session_method_pref(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType,
            [MarshalAs(UnmanagedType.LPStr)] string prefs);


        //---------------------------------------------------------------------
        // Handshake functions.
        //---------------------------------------------------------------------

        //
        // NB. This function hangs when using libssh2 1.9.0 on Windows 10 1903.
        // https://github.com/libssh2/libssh2/issues/388
        //
        [DllImport(Libssh2)]
        public static extern Int32 libssh2_session_handshake(
            SshSessionHandle session,
            IntPtr socket);

        //---------------------------------------------------------------------
        // Hostkey functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern IntPtr libssh2_hostkey_hash(
            SshSessionHandle session,
            LIBSSH2_HOSTKEY_HASH hashType);

        public static void Call(Func<Int32> function)
        {
            LIBSSH2_ERROR result = 0;
            do
            {
                result = (LIBSSH2_ERROR) function();
            }
            while (result == LIBSSH2_ERROR.EAGAIN);

            if (result != LIBSSH2_ERROR.NONE)
            {
                throw new SshNativeException(result);
            }
        }

        //---------------------------------------------------------------------
        // Tracing functions.
        //---------------------------------------------------------------------
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TraceHandler(
            IntPtr sessionHandle,
            IntPtr context,
            IntPtr data,
            Int32 length);

        [DllImport(Libssh2)]
        public static extern void libssh2_trace(
            SshSessionHandle session,
            LIBSSH2_TRACE bitmask);


        [DllImport(Libssh2)]
        public static extern void libssh2_trace_sethandler(
            SshSessionHandle session,
            IntPtr context,
            TraceHandler callback);
    }

    internal class SshSessionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SshSessionHandle() : base(true)
        {
            // Safe handle "owns" the handle.
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            UnsafeNativeMethods.Call(
                () => UnsafeNativeMethods.libssh2_session_free(handle));
            return true;
        }
    }
}
