using Google.Apis.Util;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected Libssh2 session.
    /// </summary>
    public class SshConnectedSession : IDisposable
    {
        private readonly SshSessionHandle sessionHandle;
        private readonly Socket socket;
        private bool disposed = false;

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

        internal SshConnectedSession(SshSessionHandle sessionHandle, Socket socket)
        {
            this.sessionHandle = sessionHandle;
            this.socket = socket;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public string GetRemoteBanner()
        {
            lock (this.sessionHandle.SyncRoot)
            {
                var bannerPtr = UnsafeNativeMethods.libssh2_session_banner_get(
                    this.sessionHandle);

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
            if (!Enum.IsDefined(typeof(LIBSSH2_METHOD), methodType))
            {
                throw new ArgumentException(nameof(methodType));
            }

            lock (this.sessionHandle.SyncRoot)
            {
                var stringPtr = UnsafeNativeMethods.libssh2_session_methods(
                    this.sessionHandle,
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
            if (!Enum.IsDefined(typeof(LIBSSH2_HOSTKEY_HASH), hashType))
            {
                throw new ArgumentException(nameof(hashType));
            }

            lock (this.sessionHandle.SyncRoot)
            {
                var hashPtr = UnsafeNativeMethods.libssh2_hostkey_hash(
                    this.sessionHandle,
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
            lock (this.sessionHandle.SyncRoot)
            {
                var keyPtr = UnsafeNativeMethods.libssh2_session_hostkey(
                    this.sessionHandle,
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
            lock (this.sessionHandle.SyncRoot)
            {
                var keyPtr = UnsafeNativeMethods.libssh2_session_hostkey(
                    this.sessionHandle,
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
                lock (this.sessionHandle.SyncRoot)
                {
                    return UnsafeNativeMethods.libssh2_userauth_authenticated(
                      this.sessionHandle) == 1;
                }
            }
        }

        public string[] GetAuthenticationMethods(string username)
        {
            lock (this.sessionHandle.SyncRoot)
            {
                var stringPtr = UnsafeNativeMethods.libssh2_userauth_list(
                    this.sessionHandle,
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

        public Task<SshAuthenticatedSession> Authenticate(
            string username,
            RSACng key)
        {
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
                Debug.Assert(session == this.sessionHandle.DangerousGetHandle());
                
                //
                // Copy data to managed buffer and create signature.
                //
                var data = new byte[dataLength.ToInt32()];
                Marshal.Copy(dataPtr, data, 0, data.Length);

                var signature = key.SignData(
                    data,
                    HashAlgorithmName.SHA1,     // TODO: always use SHA-1?
                    RSASignaturePadding.Pkcs1); // TODO: always use PKCS#1?

                //
                // Copy data back to a buffer that libssh2 can free using
                // the allocator specified in libssh2_session_init_ex.
                //

                signatureLength = new IntPtr(signature.Length);
                signaturePtr = SshSession.AllocDelegate(signatureLength, IntPtr.Zero);
                Marshal.Copy(signature, 0, signaturePtr, signature.Length);
                
                return (int)LIBSSH2_ERROR.NONE;
            }

            return Task.Run(() =>
            {
                //
                // NB. The public key must be passed in OpenSSH format, not PEM.
                // cf. https://tools.ietf.org/html/rfc4253#section-6.6
                //
                var publicKey = key.ToSshPublicKey();
                lock (this.sessionHandle.SyncRoot)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_userauth_publickey(
                        this.sessionHandle,
                        username,
                        publicKey,
                        new IntPtr(publicKey.Length),
                        Sign,
                        IntPtr.Zero);
                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw new SshNativeException(result);
                    }
                    else
                    {
                        return new SshAuthenticatedSession(this.sessionHandle);
                    }
                }
            });
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
                lock (this.sessionHandle.SyncRoot)
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_disconnect_ex(
                        this.sessionHandle,
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
}
