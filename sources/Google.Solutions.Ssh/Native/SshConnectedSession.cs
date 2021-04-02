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

using Google.Apis.Util;
using Google.Solutions.Common.Diagnostics;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected Libssh2 session.
    /// </summary>
    public class SshConnectedSession : IDisposable
    {
        // NB. This object does not own this handle and should not dispose it.
        private readonly SshSession session;

        private readonly Socket socket;
        private bool disposed = false;

        public Socket Socket => this.socket;

        private static int HostKeyHashLength(LIBSSH2_HOSTKEY_HASH hashType)
        {
            switch (hashType)
            {
                case LIBSSH2_HOSTKEY_HASH.MD5:
                    return 16;

                case LIBSSH2_HOSTKEY_HASH.SHA1:
                    return 16;

                case LIBSSH2_HOSTKEY_HASH.SHA256:
                    return 32;

                default:
                    throw new ArgumentException(nameof(hashType));
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshConnectedSession(SshSession session, Socket socket)
        {
            this.session = session;
            this.socket = socket;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public string GetRemoteBanner()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var bannerPtr = UnsafeNativeMethods.libssh2_session_banner_get(
                    this.session.Handle);

                return bannerPtr == IntPtr.Zero
                    ? null
                    : Marshal.PtrToStringAnsi(bannerPtr);
            }
        }


        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        public string[] GetActiveAlgorithms(LIBSSH2_METHOD methodType)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_METHOD), methodType))
            {
                throw new ArgumentException(nameof(methodType));
            }

            using (SshTraceSources.Default.TraceMethod().WithParameters(methodType))
            {
                var stringPtr = UnsafeNativeMethods.libssh2_session_methods(
                    this.session.Handle,
                    methodType);

                if (stringPtr == IntPtr.Zero)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    var algorithmList = Marshal.PtrToStringAnsi(stringPtr);
                    return algorithmList.Split(',').ToArray();
                }
            }
        }

        //---------------------------------------------------------------------
        // Host key.
        //---------------------------------------------------------------------

        public byte[] GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH hashType)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_HOSTKEY_HASH), hashType))
            {
                throw new ArgumentException(nameof(hashType));
            }

            using (SshTraceSources.Default.TraceMethod().WithParameters(hashType))
            {
                var hashPtr = UnsafeNativeMethods.libssh2_hostkey_hash(
                    this.session.Handle,
                    hashType);

                if (hashPtr == IntPtr.Zero)
                {
                    return null;
                }
                else
                {
                    var hash = new byte[HostKeyHashLength(hashType)];
                    Marshal.Copy(hashPtr, hash, 0, hash.Length);
                    return hash;
                }
            }
        }

        public byte[] GetRemoteHostKey()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var keyPtr = UnsafeNativeMethods.libssh2_session_hostkey(
                    this.session.Handle,
                    out var keyLength,
                    out var _);

                if (keyPtr == IntPtr.Zero || keyLength.ToInt32() <= 0)
                {
                    return null;
                }
                else
                {
                    var key = new byte[keyLength.ToInt32()];
                    Marshal.Copy(keyPtr, key, 0, keyLength.ToInt32());
                    return key;
                }
            }
        }

        public LIBSSH2_HOSTKEY_TYPE GetRemoteHostKeyType()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithoutParameters())
            {
                var keyPtr = UnsafeNativeMethods.libssh2_session_hostkey(
                    this.session.Handle,
                    out var _,
                    out var type);

                if (keyPtr == IntPtr.Zero)
                {
                    return LIBSSH2_HOSTKEY_TYPE.UNKNOWN;
                }
                else
                {
                    return type;
                }
            }
        }

        //---------------------------------------------------------------------
        // User auth.
        //---------------------------------------------------------------------

        public bool IsAuthenticated
        {
            get
            {
                using (SshTraceSources.Default.TraceMethod().WithoutParameters())
                {
                    this.session.Handle.CheckCurrentThreadOwnsHandle();

                    return UnsafeNativeMethods.libssh2_userauth_authenticated(
                        this.session.Handle) == 1;
                }
            }
        }

        public string[] GetAuthenticationMethods(string username)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSources.Default.TraceMethod().WithParameters(username))
            {
                var stringPtr = UnsafeNativeMethods.libssh2_userauth_list(
                    this.session.Handle,
                    username,
                    username.Length);

                if (stringPtr == IntPtr.Zero)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    return Marshal
                        .PtrToStringAnsi(stringPtr)
                        .Split(',')
                        .ToArray();
                }
            }
        }

        public SshAuthenticatedSession Authenticate(
            string username,
            ISshKey key)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            Utilities.ThrowIfNullOrEmpty(username, nameof(username));
            Utilities.ThrowIfNull(key, nameof(key));

            //
            // NB. The callbacks very sparsely documented in the libssh2 sources
            // and docs. For sample usage, the Guacamole sources can be helpful, cf.
            // https://github.com/stuntbadger/GuacamoleServer/blob/a06ae0743b0609cde0ceccc7ed136b0d71009105/src/common-ssh/ssh.c#L335
            //

            int Sign(
                IntPtr session,
                out IntPtr signaturePtr,
                out IntPtr signatureLength,
                IntPtr dataPtr,
                IntPtr dataLength,
                IntPtr context)
            {
                Debug.Assert(context == IntPtr.Zero);
                Debug.Assert(session == this.session.Handle.DangerousGetHandle());

                //
                // Copy data to managed buffer and create signature.
                //
                var data = new byte[dataLength.ToInt32()];
                Marshal.Copy(dataPtr, data, 0, data.Length);

                var signature = key.SignData(data);

                //
                // Copy data back to a buffer that libssh2 can free using
                // the allocator specified in libssh2_session_init_ex.
                //

                signatureLength = new IntPtr(signature.Length);
                signaturePtr = SshSession.Alloc(signatureLength, IntPtr.Zero);
                Marshal.Copy(signature, 0, signaturePtr, signature.Length);

                return (int)LIBSSH2_ERROR.NONE;
            }

            using (SshTraceSources.Default.TraceMethod().WithParameters(username))
            {
                //
                // NB. The public key must be passed in OpenSSH format, not PEM.
                // cf. https://tools.ietf.org/html/rfc4253#section-6.6
                //
                var publicKey = key.PublicKey;

                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_userauth_publickey(
                    this.session.Handle,
                    username,
                    key.PublicKey,
                    new IntPtr(publicKey.Length),
                    Sign,
                    IntPtr.Zero);

                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw this.session.CreateException(result);
                }
                else
                {
                    return new SshAuthenticatedSession(this.session);
                }
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
                this.session.Handle.CheckCurrentThreadOwnsHandle();

                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_disconnect_ex(
                    this.session.Handle,
                    SSH_DISCONNECT.BY_APPLICATION,
                    null,
                    null);

                Debug.Assert(result == LIBSSH2_ERROR.NONE);
                this.socket.Dispose();
                this.disposed = true;
            }
        }
    }
}
