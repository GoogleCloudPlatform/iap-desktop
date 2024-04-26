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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    internal class Libssh2Session : IDisposable
    {
        internal const string BannerPrefix = "SSH-2.0-";

        private Libssh2SessionHandle? sessionHandle;
        private bool disposed = false;

        private string? banner = null;
        private bool blocking;
        private TimeSpan timeout = TimeSpan.Zero;
        private NativeMethods.TraceHandler? traceHandlerDelegate;
        private LIBSSH2_TRACE traceMask = (LIBSSH2_TRACE)0;
        private readonly Dictionary<LIBSSH2_METHOD, string[]> preferredMethods 
            = new Dictionary<LIBSSH2_METHOD, string[]>();

        internal static readonly NativeMethods.Alloc Alloc;
        internal static readonly NativeMethods.Free Free;
        internal static readonly NativeMethods.Realloc Realloc;

        //---------------------------------------------------------------------
        // Initialization.
        //---------------------------------------------------------------------

        static Libssh2Session()
        {
            //
            // Store these delegates in fields to prevent them from being
            // garbage collected. Otherwise callbacks will suddenly
            // start hitting GC'ed memory.
            //
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
            //
            // Use blocking I/O by default.
            //
            this.blocking = true;
        }

        /// <summary>
        /// Lazily initialize a Libssh2 session.
        /// </summary>
        private void InitializeSession(bool force)
        {
            using (SshTraceSource.Log.TraceMethod().WithParameters(force))
            {
                if (this.sessionHandle != null && force)
                {
                    //
                    // Close existing session to force re-initialization.
                    //
                    this.sessionHandle.CheckCurrentThreadOwnsHandle();
                    this.sessionHandle.Dispose();
                    this.sessionHandle = null;
                }

                if (this.sessionHandle == null)
                {
                    this.sessionHandle = NativeMethods.libssh2_session_init_ex(
                        Alloc,
                        Free,
                        Realloc,
                        IntPtr.Zero);

                    if (this.traceHandlerDelegate != null)
                    {
                        //
                        // NB. We must not pass a delegate to libssh2_trace_sethandler
                        // as it might be garbage-collected while still being referenced
                        // by native code.
                        //

                        NativeMethods.libssh2_trace_sethandler(
                            this.sessionHandle,
                            IntPtr.Zero,
                            this.traceHandlerDelegate);

                        NativeMethods.libssh2_trace(
                            this.sessionHandle,
                            this.traceMask);
                    }

                    NativeMethods.libssh2_session_set_timeout(
                        this.sessionHandle,
                        (int)this.timeout.TotalMilliseconds);

                    NativeMethods.libssh2_session_set_blocking(
                        this.sessionHandle,
                        this.blocking ? 1 : 0);

                    if (this.banner != null)
                    {
                        _ = NativeMethods.libssh2_session_banner_set(
                            this.sessionHandle,
                            this.banner);
                    }

                    foreach (var preferredMethod in this.preferredMethods)
                    {
                        var prefs = string.Join(",", preferredMethod.Value);
                        var result = (LIBSSH2_ERROR)NativeMethods.libssh2_session_method_pref(
                            this.sessionHandle,
                            preferredMethod.Key,
                            prefs);
                        if (result != LIBSSH2_ERROR.NONE)
                        {
                            throw CreateException(result);
                        }
                    }
                }

                Debug.Assert(this.sessionHandle != null);
            }
        }

        internal Libssh2SessionHandle Handle
        {
            get => this.sessionHandle ?? 
                throw new InvalidOperationException("The session has not been initialized");
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        public bool IsBlocking
        {
            get => this.blocking;
            private set
            {
                this.blocking = value;
                if (this.sessionHandle != null)
                {
                    //
                    // Apply to existing session.
                    //
                    this.sessionHandle.CheckCurrentThreadOwnsHandle();
                    NativeMethods.libssh2_session_set_blocking(
                        this.sessionHandle,
                        value ? 1 : 0);
                }
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

        /// <summary>
        /// Query the list of supported algorithms.
        /// 
        /// This forces the session to be initialized.
        /// </summary>
        internal string[] GetSupportedAlgorithms(LIBSSH2_METHOD methodType)
        {
            //
            // Initialize session if that hasn't happened yet.
            //
            InitializeSession(false);

            this.sessionHandle!.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_METHOD), methodType))
            {
                throw new ArgumentException("The method is not supported");
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
            Precondition.ExpectNotNullOrZeroSized(methods, nameof(methods));
            Precondition.Expect(
                this.sessionHandle == null,
                "Method must be called before the session is initialized");

            this.preferredMethods[methodType] = methods;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public void SetLocalBanner(string banner) // TODO: Convert to property
        {
            if (!banner.StartsWith(BannerPrefix))
            {
                throw new ArgumentException(
                    $"Banner must start with '{BannerPrefix}'");
            }

            this.banner = banner;
        }

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        /// <summary>
        /// Timeout for blocking operations.
        /// </summary>
        public TimeSpan Timeout
        {
            get => this.timeout;
            set
            {
                this.timeout = value;

                //
                // Update existing session.
                //
                if (this.sessionHandle != null)
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
        // Handshake.
        //---------------------------------------------------------------------

        private const ushort MaxKexRetries = 3;

        public Libssh2ConnectedSession Connect(EndPoint remoteEndpoint)
        {
            using (SshTraceSource.Log.TraceMethod().WithParameters(remoteEndpoint))
            {
                for (int kexAttempt = 0; ; kexAttempt++)
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

                    //
                    // Initialize session if that hasn't happened yet.
                    //
                    InitializeSession(kexAttempt > 0);
                    this.sessionHandle!.CheckCurrentThreadOwnsHandle();

                    SshEventSource.Log.ConnectionHandshakeInitiated(remoteEndpoint.ToString());

                    socket.Connect(remoteEndpoint);

                    var result = (LIBSSH2_ERROR)NativeMethods.libssh2_session_handshake(
                        this.sessionHandle,
                        socket.Handle);

                    if (result == LIBSSH2_ERROR.KEY_EXCHANGE_FAILURE && kexAttempt < MaxKexRetries)
                    {
                        //
                        // When using the WinCNG backend, key exchanges can occasionally fail,
                        // see https://github.com/libssh2/libssh2/issues/804.
                        //
                        // Retry a few times.
                        //

                        SshTraceSource.Log.TraceWarning("KEX failed, retrying...");
                        socket.Close();
                    }
                    else if (result != LIBSSH2_ERROR.NONE)
                    {
                        //
                        // Some other error occured, don't retry.
                        //
                        socket.Close();
                        throw CreateException(result);
                    }
                    else
                    {
                        SshEventSource.Log.ConnectionHandshakeCompleted(remoteEndpoint.ToString());

                        return new Libssh2ConnectedSession(this, socket);
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Error.
        //---------------------------------------------------------------------

        internal LIBSSH2_ERROR LastError
        {
            get
            {
                if (this.sessionHandle == null)
                {
                    return LIBSSH2_ERROR.NONE;
                }
                else
                {
                    this.sessionHandle.CheckCurrentThreadOwnsHandle();

                    return (LIBSSH2_ERROR)
                        NativeMethods.libssh2_session_last_errno(this.sessionHandle);
                }
            }
        }

        internal Libssh2Exception CreateException(LIBSSH2_ERROR error)
        {
            if (this.sessionHandle != null)
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
            }

            //
            // Fall back to using a generic error message.
            //
            return new Libssh2Exception(
                error,
                $"SSH operation failed: {error}");
        }

        //---------------------------------------------------------------------
        // Tracing.
        //---------------------------------------------------------------------

        internal void SetTraceHandler(
            LIBSSH2_TRACE mask,
            Action<string> handler)
        {
            this.traceHandlerDelegate = (sessionPtr, contextPtr, dataPtr, length) =>
            {
                Debug.Assert(contextPtr == IntPtr.Zero);

                var data = new byte[length.ToInt32()];
                Marshal.Copy(dataPtr, data, 0, length.ToInt32());

                handler(Encoding.ASCII.GetString(data));
            };
            this.traceMask = mask;
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.sessionHandle != null)
                {
                    this.sessionHandle.CheckCurrentThreadOwnsHandle();

                    NativeMethods.libssh2_trace_sethandler(
                        this.sessionHandle,
                        IntPtr.Zero,
                        null);

                    this.sessionHandle.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
