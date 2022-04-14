﻿//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#pragma warning disable IDE1006 // Naming Styles
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
        KEX = 0,
        HOSTKEY = 1,
        CRYPT_CS = 2, // Client -> Server
        CRYPT_SC = 3, // Server -> Client
        MAC_CS = 4,   // Client -> Server
        MAC_SC = 5,   // Server -> Client
        COMP_CS = 6,  // Compression Client -> Server
        COMP_SC = 7,  // Compression Server -> Client
        LANG_CS = 8,  // Client -> Server
        LANG_SC = 9,  // Server -> Client
    }

    [Flags]
    [SuppressMessage("Naming", "CA1714:Flags enums should have plural names")]
    public enum LIBSSH2_TRACE : Int32
    {
        TRANS = (1 << 1),
        KEX = (1 << 2),
        AUTH = (1 << 3),
        CONN = (1 << 4),
        SCP = (1 << 5),
        SFTP = (1 << 6),
        ERROR = (1 << 7),
        internalKEY = (1 << 8),
        SOCKET = (1 << 9)
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

    internal enum SSH_DISCONNECT : Int32
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

    public enum LIBSSH2_CHANNEL_EXTENDED_DATA : Int32
    {
        NORMAL = 0,
        IGNORE = 1,
        MERGE = 2,
    }

    public enum LIBSSH2_STREAM : Int32
    {
        NORMAL = 0,
        EXTENDED_DATA_STDERR = 1
    }

    /// <summary>
    /// FTP File Transfer Flags.
    /// </summary>
    [Flags]
    public enum LIBSSH2_FXF_FLAGS : Int32
    {
        READ   = 0x00000001,
        WRITE  = 0x00000002,
        APPEND = 0x00000004,
        CREAT  = 0x00000008,
        TRUNC  = 0x00000010,
        EXCL   = 0x00000020
    }

    internal enum LIBSSH2_OPENTYPE : Int32
    {
        OPENFILE = 0,
        OPENDIR  = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LIBSSH2_SFTP_ATTRIBUTES
    {
        /// <summary>
        /// If flags & ATTR_* bit is set, then the value in this struct will be
        /// meaningful Otherwise it should be ignored
        /// </summary>
        internal uint flags;
        internal ulong filesize;
        internal uint uid;
        internal uint gid;
        internal uint permissions;
        internal uint atime;
        internal uint mtime;
    };

    internal static class UnsafeNativeMethods
    {
        private const string Libssh2 = "libssh2.dll";

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_init(Int32 flags);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr libssh2_version(
            int requiredVersion);

        //---------------------------------------------------------------------
        // Session functions.
        //---------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr Alloc(
            IntPtr size,
            IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr Realloc(
            IntPtr ptr,
            IntPtr size,
            IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void Free(
            IntPtr ptr,
            IntPtr context);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern SshSessionHandle libssh2_session_init_ex(
            Alloc alloc,
            Free free,
            Realloc realloc,
            IntPtr context);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_free(
            SshSessionHandle session,
            IntPtr ptr);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_session_free(
            IntPtr session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern Int32 libssh2_session_disconnect_ex(
            SshSessionHandle session,
            SSH_DISCONNECT reason,
            [MarshalAs(UnmanagedType.LPStr)] string description,
            [MarshalAs(UnmanagedType.LPStr)] string lang);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_session_get_blocking(
            SshSessionHandle session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void libssh2_session_set_blocking(
            SshSessionHandle session,
            Int32 blocking);

        //---------------------------------------------------------------------
        // Algorithm functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr libssh2_session_methods(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_session_supported_algs(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType,
            [Out] out IntPtr algorithmsPtrPtr);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern Int32 libssh2_session_method_pref(
            SshSessionHandle session,
            LIBSSH2_METHOD methodType,
            [MarshalAs(UnmanagedType.LPStr)] string prefs);


        //---------------------------------------------------------------------
        // Banner functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr libssh2_session_banner_get(
            SshSessionHandle session);


        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern Int32 libssh2_session_banner_set(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string banner);

        //---------------------------------------------------------------------
        // Handshake functions.
        //---------------------------------------------------------------------

        //
        // NB. This function hangs when using libssh2 1.9.0 on Windows 10 1903.
        // https://github.com/libssh2/libssh2/issues/388
        //
        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_session_handshake(
            SshSessionHandle session,
            IntPtr socket);

        //---------------------------------------------------------------------
        // Hostkey functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr libssh2_session_hostkey(
            SshSessionHandle session,
            out IntPtr length,
            out LIBSSH2_HOSTKEY_TYPE type);


        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr libssh2_hostkey_hash(
            SshSessionHandle session,
            LIBSSH2_HOSTKEY_HASH hashType);

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_session_get_timeout(
            SshSessionHandle session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void libssh2_session_set_timeout(
            SshSessionHandle session,
            int timeout);

        //---------------------------------------------------------------------
        // User auth.
        //
        // NB. The documentation on libssh2_userauth_publickey is extremely sparse.
        // For a usage example, see:
        // https://github.com/stuntbadger/GuacamoleServer/blob/master/src/common-ssh/ssh.c
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 libssh2_userauth_authenticated(
            SshSessionHandle session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr libssh2_userauth_list(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string username,
            int usernameLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int SignCallback(
            IntPtr session,
            out IntPtr signature,
            out IntPtr signatureLength,
            IntPtr data,
            IntPtr dataLength,
            IntPtr context);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int libssh2_userauth_publickey(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string username,
            [MarshalAs(UnmanagedType.LPArray)] byte[] publicKey,
            IntPtr pemPublicKeyLength,
            SignCallback callback,
            IntPtr context);

        [StructLayout(LayoutKind.Sequential)]
        internal struct LIBSSH2_USERAUTH_KBDINT_PROMPT
        {
            internal IntPtr TextPtr;
            internal int TextLength;
            internal byte Echo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LIBSSH2_USERAUTH_KBDINT_RESPONSE
        {
            internal IntPtr TextPtr;
            internal int TextLength;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void KeyboardInteractiveCallback(
            IntPtr namePtr,
            int nameLength,
            IntPtr instructionPtr,
            int instructionLength,
            int numPrompts,
            IntPtr prompts,
            IntPtr responses,
            IntPtr context);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int libssh2_userauth_keyboard_interactive_ex(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string username,
            int usernameLength,
            KeyboardInteractiveCallback callback,
            IntPtr context);

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_close(
            SshChannelHandle channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_free(
            IntPtr channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern SshChannelHandle libssh2_channel_open_ex(
            SshSessionHandle session,
            [MarshalAs(UnmanagedType.LPStr)] string channelType,
            uint channelTypeLength,
            uint windowSize,
            uint packetSize,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            uint messageLength);


        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int libssh2_channel_setenv_ex(
            SshChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            uint variableNameLength,
            [MarshalAs(UnmanagedType.LPStr)] string variableValue,
            uint variableValueLength);


        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int libssh2_channel_process_startup(
            SshChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string request,
            uint requestLength,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            uint messageLength);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_read_ex(
            SshChannelHandle channel,
            int streamId,
            byte[] buffer,
            IntPtr bufferSize);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_write_ex(
            SshChannelHandle channel,
            int streamId,
            byte[] buffer,
            IntPtr bufferSize);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_wait_closed(
            SshChannelHandle channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_wait_eof(
            SshChannelHandle channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_get_exit_status(
            SshChannelHandle channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_get_exit_signal(
            SshChannelHandle channel,
            out IntPtr exitsignal,
            out IntPtr exitsignalLength,
            out IntPtr errmsg,
            out IntPtr errmsgLength,
            out IntPtr langTag,
            out IntPtr langTagLength);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_handle_extended_data2(
            SshChannelHandle channel,
            LIBSSH2_CHANNEL_EXTENDED_DATA mode);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_eof(
            SshChannelHandle channel);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int libssh2_channel_request_pty_ex(
            SshChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string term,
            uint termLength,
            byte[] modes,
            uint modesLength,
            int width,
            int height,
            int widthPx,
            int heightPx);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_channel_request_pty_size_ex(
            SshChannelHandle channel,
            int width,
            int height,
            int widthPx,
            int heightPx);

        //---------------------------------------------------------------------
        // SFTP.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern SshSftpChannelHandle libssh2_sftp_init(
            SshSessionHandle session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_shutdown(
            IntPtr sftp);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_last_error(
            SshSftpChannelHandle sftp);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_close_handle(
            IntPtr sftp);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern SshSftpFileHandle libssh2_sftp_open_ex(
            SshSftpChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            uint pathLength,
            LIBSSH2_FXF_FLAGS flags,
            FilePermissions mode,
            LIBSSH2_OPENTYPE openType);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_readdir_ex(
            SshSftpFileHandle handle,
            IntPtr buffer,
            IntPtr bufferSize,
            IntPtr longEntry,
            IntPtr longEntrySize,
            out LIBSSH2_SFTP_ATTRIBUTES attrs);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_mkdir_ex(
            SshSftpChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            uint pathLength,
            FilePermissions mode);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_rmdir_ex(
            SshSftpChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            uint pathLength);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_unlink_ex(
            SshSftpChannelHandle channel,
            [MarshalAs(UnmanagedType.LPStr)] string path,
            uint pathLength);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_read(
            SshSftpFileHandle channel,
            byte[] buffer,
            IntPtr bufferSize);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_sftp_write(
            SshSftpFileHandle channel,
            byte[] buffer,
            IntPtr bufferSize);

        //---------------------------------------------------------------------
        // Keepalive.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void libssh2_keepalive_config(
            SshSessionHandle session,
            int wantReply,
            uint interval);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_keepalive_send(
            SshSessionHandle session,
            out int secondsToNext);

        //---------------------------------------------------------------------
        // Error functions.
        //---------------------------------------------------------------------

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_session_last_errno(
            SshSessionHandle session);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int libssh2_session_last_error(
            SshSessionHandle session,
            out IntPtr errorMessage,
            out int errorMessageLength,
            int allocateBuffer);

        //---------------------------------------------------------------------
        // Tracing functions.
        //---------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void TraceHandler(
            IntPtr sessionHandle,
            IntPtr context,
            IntPtr data,
            IntPtr length);

        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void libssh2_trace(
            SshSessionHandle session,
            LIBSSH2_TRACE bitmask);


        [DllImport(Libssh2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void libssh2_trace_sethandler(
            SshSessionHandle session,
            IntPtr context,
            TraceHandler callback);

        //---------------------------------------------------------------------
        // Winsock.
        //---------------------------------------------------------------------

        internal const uint WSA_WAIT_FAILED = 0xFFFFFFFF;
        internal const uint WSA_WAIT_EVENT_0 = 0;
        internal const uint WSA_WAIT_TIMEOUT = 0x102;
        internal const uint FD_READ = 1;
        internal const uint FD_WRITE = 2;
        internal const uint FD_OOB = 4;
        internal const uint FD_ACCEPT = 8;
        internal const uint FD_CONNECT = 16;
        internal const uint FD_CLOSE = 32;

        [DllImport("Ws2_32.dll")]
        internal extern static WsaEventHandle WSACreateEvent();

        [DllImport("Ws2_32.dll")]
        internal extern static int WSAEventSelect(
            IntPtr socket,
            WsaEventHandle hande,
            uint eventMask);

        [DllImport("Ws2_32.dll")]
        internal extern static bool WSASetEvent(WsaEventHandle hande);

        [DllImport("Ws2_32.dll")]
        internal extern static bool WSAResetEvent(WsaEventHandle hande);

        [DllImport("Ws2_32.dll")]
        internal extern static bool WSACloseEvent(IntPtr hande);

        [DllImport("Ws2_32.dll")]
        internal extern static uint WSAWaitForMultipleEvents(
            uint cEvents,
            IntPtr[] pEvents,
            bool fWaitAll,
            uint timeout,
            bool fAlterable);

        [DllImport("Ws2_32.dll")]
        internal extern static int WSAEnumNetworkEvents(
            IntPtr socket,
            WsaEventHandle eventHandle,
            ref WSANETWORKEVENTS eventInfo);

        [DllImport("Ws2_32.dll")]
        internal extern static int WSAGetLastError();


        [StructLayout(LayoutKind.Sequential)]
        internal struct WSANETWORKEVENTS
        {
            internal int lNetworkEvents;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            internal int[] iErrorCode;
        }

        //---------------------------------------------------------------------
        // Utility functions.
        //---------------------------------------------------------------------

        internal static string PtrToString(
            IntPtr stringPtr,
            int stringLength,
            Encoding encoding)
        {
            if (stringPtr == IntPtr.Zero || stringLength == 0)
            {
                return null;
            }

            var buffer = new byte[stringLength];
            Marshal.Copy(stringPtr, buffer, 0, stringLength);
            return encoding.GetString(buffer);
        }

        internal static T[] PtrToStructureArray<T>(
            IntPtr ptr,
            int count) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                IntPtr elementPtr = new IntPtr(ptr.ToInt64() + i * size);
                array[i] = Marshal.PtrToStructure<T>(elementPtr);
            }

            return array;
        }

        internal static void StructureArrayToPtr<T>(
            IntPtr ptr,
            T[] array) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < array.Length; i++)
            {
                IntPtr elementPtr = new IntPtr(ptr.ToInt64() + i * size);
                Marshal.StructureToPtr(array[i], elementPtr, false);
            }
        }
    }

    //-------------------------------------------------------------------------
    // Safe handles.
    //-------------------------------------------------------------------------

    /// <summary>
    /// Safe handle for a LIBSSH2_SESSION.
    /// </summary>
    internal class SshSessionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
#if DEBUG
        private readonly Thread owningThread = Thread.CurrentThread;
#endif

        private SshSessionHandle() : base(true)
        {
            HandleTable.OnHandleCreated(this, "SSH session");
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_free(
                this.handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);

            HandleTable.OnHandleClosed(this);

            return true;
        }

        [Conditional("DEBUG")]
        internal void CheckCurrentThreadOwnsHandle()
        {
#if DEBUG
            Debug.Assert(Thread.CurrentThread == this.owningThread);
#endif
        }
    }

    /// <summary>
    /// Safe handle for a resource that's dependent on a LIBSSH2_SESSION.
    /// </summary>
    internal abstract class SshSessionResourceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
#if DEBUG
        private readonly Thread owningThread = Thread.CurrentThread;
#endif

        /// <summary>
        /// Handle to parent session.
        /// </summary>
        internal SshSessionHandle SessionHandle { get; private set; }

        protected SshSessionResourceHandle() : base(true)
        {
            HandleTable.OnHandleCreated(this, "SSH session");
        }

        [Conditional("DEBUG")]
        internal void CheckCurrentThreadOwnsHandle()
        {
#if DEBUG
            Debug.Assert(Thread.CurrentThread == this.owningThread);
#endif
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            //
            // NB. Libssh2 manages channels and SFTPs as a sub-resource of a
            // session.
            //
            // Freeing a sub-resource is fine if the session is still around,
            // but trying to free a sub-resource *after* freeing a session will
            // cause an access violation.
            //
            // When calling dispose, it's therefore imporant to dispose
            // sub-resource before disposing their parent session - that's
            // what the following assertion is for.
            //
            // When handles are not disposed (for example, because the app
            // is being force-closed), then the finalizer may release
            // resources in arbitrary order. 
            //
            Debug.Assert(this.SessionHandle != null);
            Debug.Assert(!this.SessionHandle.IsClosed);

            if (this.SessionHandle.IsClosed)
            {
                // Do not free the channel.
                return false;
            }

            //
            // Release the actual handle.
            //
            ProtectedReleaseHandle();

            HandleTable.OnHandleClosed(this);

            return true;
        }

        internal void ValidateAndAttachToSession(SshSession session)
        {
            Debug.Assert(session != null);

            if (this.IsInvalid)
            {
                throw session.CreateException(
                    (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_last_errno(
                        session.Handle));
            }

            this.SessionHandle = session.Handle;
        }

        protected abstract void ProtectedReleaseHandle();
    }

    /// <summary>
    /// Safe handle for LIBSSH2_CHANNEL.
    /// </summary>
    internal class SshChannelHandle : SshSessionResourceHandle
    {
        protected override void ProtectedReleaseHandle()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_channel_free(
                this.handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);
        }
    }

    /// <summary>
    /// Safe handle for LIBSSH2_SFTP.
    /// </summary>
    internal class SshSftpChannelHandle : SshSessionResourceHandle
    {
        protected override void ProtectedReleaseHandle()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_sftp_shutdown(
                this.handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);
        }
    }
    /// <summary>
    /// Safe handle for LIBSSH2_SFTP_HANDLE.
    /// </summary>
    internal class SshSftpFileHandle : SshSessionResourceHandle
    {
        protected override void ProtectedReleaseHandle()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_sftp_close_handle(
                this.handle);
            Debug.Assert(result == LIBSSH2_ERROR.NONE);
        }
    }

    internal class WsaEventHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private WsaEventHandle() : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            bool result = UnsafeNativeMethods.WSACloseEvent(this.handle);
            Debug.Assert(result);
            return result;
        }
    }

    internal sealed class GlobalAllocSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static GlobalAllocSafeHandle Zero = new GlobalAllocSafeHandle(ownsHandle: false);

        private GlobalAllocSafeHandle()
            : base(ownsHandle: true)
        {
        }

        private GlobalAllocSafeHandle(bool ownsHandle)
            : base(ownsHandle)
        {
        }


        [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
        internal static GlobalAllocSafeHandle GlobalAlloc(uint cb)
        {
            var memory = Marshal.AllocHGlobal(new IntPtr(cb));

            var handle = new GlobalAllocSafeHandle();
            handle.SetHandle(memory);
            if (handle.IsInvalid)
            {
                handle.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }

            return handle;
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }
}
