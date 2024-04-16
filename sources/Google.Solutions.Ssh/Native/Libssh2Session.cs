//
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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An (unconnected) Libssh2 session.
    /// </summary>
    public class Libssh2Session : IDisposable
    {
        public const string BannerPrefix = "SSH-2.0-";

        private readonly Libssh2SessionHandle sessionHandle;
        private bool disposed = false;

        internal Libssh2SessionHandle Handle => this.sessionHandle;

        internal static readonly NativeMethods.Alloc Alloc;
        internal static readonly NativeMethods.Free Free;
        internal static readonly NativeMethods.Realloc Realloc;

        private DateTime nextKeepaliveDueTime = DateTime.Now;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        static Libssh2Session()
        {
            // Store these delegates in fields to prevent them from being
            // garbage collected. Otherwise callbacks will suddenly
            // start hitting GC'ed memory.

            Alloc = (size, context) => Marshal.AllocHGlobal(size);
            Realloc = (ptr, size, context) => Marshal.ReAllocHGlobal(ptr, size);
            Free = (ptr, context) => Marshal.FreeHGlobal(ptr);

            try
            {
                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_init(0);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw new Libssh2Exception(
                        result,
                        "Failed to initialize libssh2");
                }
            }
            catch (EntryPointNotFoundException)
            {
                throw new SshException("libssh2 DLL not found or could not be loaded");
            }
        }

        internal Libssh2Session()
        {
            this.sessionHandle = NativeMethods.libssh2_session_init_ex(
                Alloc,
                Free,
                Realloc,
                IntPtr.Zero);

            // Use blocking I/O by default.
            this.IsBlocking = true;
        }

        public static string GetVersion(Version requiredVersion)
        {
            using (SshTraceSource.Log.TraceMethod().WithParameters(requiredVersion))
            {
                var requiredVersionEncoded =
                    (requiredVersion.Major << 16) |
                    (requiredVersion.Minor << 8) |
                    (requiredVersion.Build);

                return Marshal.PtrToStringAnsi(
                    NativeMethods.libssh2_version(
                        requiredVersionEncoded));
            }
        }

        public bool IsBlocking
        {
            get
            {
                this.sessionHandle.CheckCurrentThreadOwnsHandle();
                return NativeMethods.libssh2_session_get_blocking(
                    this.sessionHandle) != 0;
            }
            private set
            {
                this.sessionHandle.CheckCurrentThreadOwnsHandle();
                NativeMethods.libssh2_session_set_blocking(
                    this.sessionHandle,
                    value ? 1 : 0);
            }
        }

        public IDisposable AsBlocking()
        {
            this.IsBlocking = true;
            return Disposable.For(() => this.IsBlocking = false);
        }

        public IDisposable AsBlocking(TimeSpan timeout)
        {
            this.IsBlocking = true;
            var previousTimeout = this.Timeout;

            this.Timeout = timeout;
            return Disposable.For(() =>
            {
                this.IsBlocking = false;
                this.Timeout = previousTimeout;
            });
        }

        public IDisposable AsNonBlocking()
        {
            this.IsBlocking = false;
            return Disposable.For(() => this.IsBlocking = true);
        }

        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        internal string[] GetSupportedAlgorithms(LIBSSH2_METHOD methodType)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_METHOD), methodType))
            {
                throw new ArgumentException(nameof(methodType));
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(methodType))
            {
                var count = NativeMethods.libssh2_session_supported_algs(
                    this.sessionHandle,
                    methodType,
                    out var algorithmsPtrPtr);
                if (count > 0 && algorithmsPtrPtr != IntPtr.Zero)
                {
                    var algorithmsPtrs = new IntPtr[count];
                    Marshal.Copy(algorithmsPtrPtr, algorithmsPtrs, 0, algorithmsPtrs.Length);

                    var algorithms = algorithmsPtrs
                        .Select(ptr => Marshal.PtrToStringAnsi(ptr))
                        .ToArray();

                    _ = NativeMethods.libssh2_free(
                        this.sessionHandle,
                        algorithmsPtrPtr);

                    return algorithms;
                }
                else if (count < 0)
                {
                    throw CreateException((LIBSSH2_ERROR)count);
                }
                else
                {
                    return Array.Empty<string>();
                }
            }
        }

        internal void SetPreferredMethods(
            LIBSSH2_METHOD methodType,
            string[] methods)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();

            Precondition.ExpectNotNullOrZeroSized(methods, nameof(methods));

            using (SshTraceSource.Log.TraceMethod().WithParameters(
                methodType,
                methods))
            {
                var prefs = string.Join(",", methods);

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_session_method_pref(
                    this.sessionHandle,
                    methodType,
                    prefs);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw CreateException(result);
                }
            }
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public void SetLocalBanner(string banner)
        {
            if (!banner.StartsWith(BannerPrefix))
            {
                throw new ArgumentException(
                    $"Banner must start with '{BannerPrefix}'");
            }

            this.sessionHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotEmpty(banner, nameof(banner));

            using (SshTraceSource.Log.TraceMethod().WithParameters(banner))
            {
                _ = NativeMethods.libssh2_session_banner_set(
                    this.sessionHandle,
                    banner);
            }
        }

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        /// <summary>
        /// Timeout for blocking operations.
        /// </summary>
        public TimeSpan Timeout
        {
            get
            {
                this.sessionHandle.CheckCurrentThreadOwnsHandle();

                using (SshTraceSource.Log.TraceMethod().WithoutParameters())
                {
                    var millis = NativeMethods.libssh2_session_get_timeout(
                        this.sessionHandle);
                    return TimeSpan.FromMilliseconds(millis);
                }
            }
            set
            {
                using (SshTraceSource.Log.TraceMethod().WithParameters(value))
                {
                    this.sessionHandle.CheckCurrentThreadOwnsHandle();

                    NativeMethods.libssh2_session_set_timeout(
                        this.sessionHandle,
                        (int)value.TotalMilliseconds);
                }
            }
        }

        public IDisposable WithTimeout(TimeSpan timeout)
        {
            var originalTimeout = this.Timeout;
            this.Timeout = timeout;
            return Disposable.For(() => this.Timeout = originalTimeout);
        }

        /// <summary>
        /// Time to wait for user to react to keyboard/interactive prompts.
        /// </summary>
        public TimeSpan KeyboardInteractivePromptTimeout { get; set; }
            = TimeSpan.FromMinutes(1);

        //---------------------------------------------------------------------
        // Keepalive.
        //---------------------------------------------------------------------

        public void ConfigureKeepAlive(
            bool wantServerResponse,
            TimeSpan interval)
        {
            using (SshTraceSource.Log.TraceMethod().WithParameters(
                wantServerResponse,
                interval))
            {
                NativeMethods.libssh2_keepalive_config(
                    this.sessionHandle,
                    wantServerResponse ? 1 : 0,
                    (uint)interval.TotalSeconds);
            }
        }

        public void KeepAlive()
        {
            if (DateTime.Now > this.nextKeepaliveDueTime)
            {
                SshTraceSource.Log.TraceVerbose("Sending keepalive");

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_keepalive_send(
                    this.sessionHandle,
                    out var secondsTillNextKeepalive);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw CreateException(result);
                }

                Debug.Assert(secondsTillNextKeepalive > 0);
                this.nextKeepaliveDueTime =
                    this.nextKeepaliveDueTime.AddSeconds(secondsTillNextKeepalive);
            }
        }

        //---------------------------------------------------------------------
        // Handshake.
        //---------------------------------------------------------------------

        public Libssh2ConnectedSession Connect(EndPoint remoteEndpoint)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithParameters(remoteEndpoint))
            {
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp)
                {
                    //
                    // Flush input data immediately so that the user does not
                    // experience a lag.
                    //
                    NoDelay = true
                };

                SshEventSource.Log.ConnectionHandshakeInitiated(remoteEndpoint.ToString());

                socket.Connect(remoteEndpoint);

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_session_handshake(
                    this.sessionHandle,
                    socket.Handle);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    socket.Close();
                    throw CreateException(result);
                }

                SshEventSource.Log.ConnectionHandshakeCompleted(remoteEndpoint.ToString());

                return new Libssh2ConnectedSession(this, socket);
            }
        }

        //---------------------------------------------------------------------
        // Error.
        //---------------------------------------------------------------------

        internal LIBSSH2_ERROR LastError
        {
            get
            {
                this.sessionHandle.CheckCurrentThreadOwnsHandle();

                return (LIBSSH2_ERROR)NativeMethods.libssh2_session_last_errno(
                    this.sessionHandle);
            }
        }

        internal Libssh2Exception CreateException(LIBSSH2_ERROR error)
        {
            var lastError = (LIBSSH2_ERROR)NativeMethods.libssh2_session_last_error(
                this.sessionHandle,
                out var errorMessage,
                out var errorMessageLength,
                0);

            SshEventSource.Log.ConnectionErrorEncountered((int)error);

            if (lastError == error)
            {
                return new Libssh2Exception(
                    error,
                    Marshal.PtrToStringAnsi(errorMessage, errorMessageLength));
            }
            else
            {
                // The last error is something else, so create a generic
                // exception
                return new Libssh2Exception(
                    error,
                    $"SSH operation failed: {error}");
            }
        }

        //---------------------------------------------------------------------
        // Tracing.
        //---------------------------------------------------------------------

        private NativeMethods.TraceHandler? TraceHandlerDelegate;

        internal void SetTraceHandler(
            LIBSSH2_TRACE mask,
            Action<string> handler)
        {
            this.sessionHandle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(handler, nameof(handler));

            using (SshTraceSource.Log.TraceMethod().WithParameters(mask))
            {
                // Store this delegate in a field to prevent it from being
                // garbage collected. Otherwise callbacks will suddenly
                // start hitting GC'ed memory.
                this.TraceHandlerDelegate = (sessionPtr, contextPtr, dataPtr, length) =>
                {
                    Debug.Assert(contextPtr == IntPtr.Zero);

                    var data = new byte[length.ToInt32()];
                    Marshal.Copy(dataPtr, data, 0, length.ToInt32());

                    handler(Encoding.ASCII.GetString(data));
                };

                NativeMethods.libssh2_trace_sethandler(
                    this.sessionHandle,
                    IntPtr.Zero,
                    this.TraceHandlerDelegate);

                NativeMethods.libssh2_trace(
                    this.sessionHandle,
                    mask);
            }
        }

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

                this.sessionHandle.CheckCurrentThreadOwnsHandle();

                NativeMethods.libssh2_trace_sethandler(
                    this.sessionHandle,
                    IntPtr.Zero,
                    null);

                this.sessionHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
