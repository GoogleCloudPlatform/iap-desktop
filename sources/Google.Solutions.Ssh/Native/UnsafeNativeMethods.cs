using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public enum LIBSSH2_HOSTKEY_TYPE : Int32
    {
        UNKNOWN = 0,
        RSA = 1,
        DSS = 2,
        ECDSA_256 = 3,
        ECDSA_384 = 4,
        ECDSA_521 = 5,
        ED25519 = 6
    }

    public enum SSH_DISCONNECT : Int32
    {
        HOST_NOT_ALLOWED_TO_CONNECT = 1,
        PROTOCOL_ERROR = 2,
        KEY_EXCHANGE_FAILED = 3,
        RESERVED = 4,
        MAC_ERROR = 5,
        COMPRESSION_ERROR = 6,
        SERVICE_NOT_AVAILABLE = 7,
        PROTOCOL_VERSION_NOT_SUPPORTED = 8,
        HOST_KEY_NOT_VERIFIABLE = 9,
        CONNECTION_LOST = 10,
        BY_APPLICATION = 11,
        TOO_MANY_CONNECTIONS = 12,
        AUTH_CANCELLED_BY_USER = 13,
        NO_MORE_AUTH_METHODS_AVAILABLE = 14,
        ILLEGAL_USER_NAME = 15
    }

    internal static class UnsafeNativeMethods
    {
        private const string Libssh2 = "libssh2.dll";

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_init(Int32 flags);

        [DllImport(Libssh2)]
        public static extern IntPtr libssh2_version(
            int requiredVersion);

        //---------------------------------------------------------------------
        // Session functions.
        //---------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Alloc(
            IntPtr size,
            IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr Realloc(
            IntPtr ptr,
            IntPtr size,
            IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Free(
            IntPtr ptr,
            IntPtr context);

        [DllImport(Libssh2)]
        public static extern SshSessionHandle libssh2_session_init_ex(
            Alloc alloc,
            Free free,
            Realloc realloc,
            IntPtr context);

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
            SSH_DISCONNECT reason,
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
        // Banner functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern IntPtr libssh2_session_banner_get(
            SshSessionHandle session);


        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern Int32 libssh2_session_banner_set(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string banner);

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

        [DllImport(Libssh2)]
        public static extern IntPtr libssh2_session_hostkey(
            SshSessionHandle session,
            out IntPtr length,
            out LIBSSH2_HOSTKEY_TYPE type);


        [DllImport(Libssh2)]
        public static extern IntPtr libssh2_hostkey_hash(
            SshSessionHandle session,
            LIBSSH2_HOSTKEY_HASH hashType);

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern int libssh2_session_get_timeout(
            SshSessionHandle session);

        [DllImport(Libssh2)]
        public static extern void libssh2_session_set_timeout(
            SshSessionHandle session,
            int timeout);

        //---------------------------------------------------------------------
        // User auth.
        //
        // NB. The documentation on libssh2_userauth_publickey is extremely sparse.
        // For a usage example, see:
        // https://github.com/stuntbadger/GuacamoleServer/blob/master/src/common-ssh/ssh.c
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern Int32 libssh2_userauth_authenticated(
            SshSessionHandle session);

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern IntPtr libssh2_userauth_list(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string username,
            int usernameLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SignCallback(
            IntPtr session, 
            out IntPtr signature, 
            out IntPtr signatureLength,
            IntPtr data, 
            IntPtr dataLength, 
            IntPtr context);

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern int libssh2_userauth_publickey(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string username,
            [MarshalAs(UnmanagedType.LPArray)] byte[] publicKey,
            IntPtr pemPublicKeyLength,
            SignCallback callback,
            IntPtr context);

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern int libssh2_channel_close(
            IntPtr channel);

        [DllImport(Libssh2)]
        public static extern int libssh2_channel_free(
            IntPtr channel);

        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern SshChannelHandle libssh2_channel_open_ex(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string channelType,
            uint channelTypeLength,
            uint windowSize,
            uint packetSize,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            uint messageLength);


        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern int libssh2_channel_setenv_ex(
            SshChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            uint variableNameLength,
            [MarshalAs(UnmanagedType.LPStr)] string variableValue,
            uint variableValueLength);


        [DllImport(Libssh2, CharSet = CharSet.Ansi)]
        public static extern int libssh2_channel_process_startup(
            SshChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string request,
            uint requestLength,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            uint messageLength);

        [DllImport(Libssh2)]
        public static extern int libssh2_channel_read_ex(
            SshChannelHandle channel,
            int streamId,
            byte[] buffer,
            IntPtr bufferSize);        
        
        [DllImport(Libssh2)]
        public static extern int libssh2_channel_write_ex(
            SshChannelHandle channel,
            int streamId,
            byte[] buffer,
            IntPtr bufferSize);

        [DllImport(Libssh2)]
        public static extern int libssh2_channel_get_exit_status(
            SshChannelHandle channel);

        [DllImport(Libssh2)]
        public static extern int libssh2_channel_get_exit_signal(
            SshChannelHandle channel,
            out IntPtr exitsignal,
            out IntPtr exitsignalLength,
            out IntPtr errmsg,
            out IntPtr errmsgLength,
            out IntPtr langTag,
            out IntPtr langTagLength);

        //---------------------------------------------------------------------
        // Error functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2)]
        public static extern int libssh2_session_last_errno(
            SshSessionHandle session);

        //---------------------------------------------------------------------
        // Tracing functions.
        //---------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TraceHandler(
            IntPtr sessionHandle,
            IntPtr context,
            IntPtr data,
            IntPtr length);

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
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_free(handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);
            return true;
        }

        /// <summary>
        /// Object to take a lock on before using the handle. Libssh2 handles
        /// are not allowed to be accessed concurrently on multiple threads.
        /// </summary>
        public object SyncRoot => new object();
    }

    internal class SshChannelHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SshChannelHandle() : base(true)
        {
            // Safe handle "owns" the handle.
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_close(handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);

            result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_free(handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);
            
            return true;
        }

        /// <summary>
        /// Object to take a lock on before using the handle. Libssh2 handles
        /// are not allowed to be accessed concurrently on multiple threads.
        /// </summary>
        public object SyncRoot => new object();
    }
}
