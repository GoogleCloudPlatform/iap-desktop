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
    public class SshConnection : IDisposable
    {
        private readonly SshSession session;
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

        internal SshConnection(SshSession session, Socket socket)
        {
            this.session = session;
            this.socket = socket;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public string GetRemoteBanner()
        {
            var bannerPtr = UnsafeNativeMethods.libssh2_session_banner_get(
                this.session.Handle);

            return bannerPtr == IntPtr.Zero
                ? null
                : Marshal.PtrToStringAnsi(bannerPtr);
        }


        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        public string[] GetActiveAlgorithms(LIBSSH2_METHOD methodType)
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

        //---------------------------------------------------------------------
        // Host key.
        //---------------------------------------------------------------------

        public byte[] GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH hashType)
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

        public byte[] GetRemoteHostKey()
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

        public LIBSSH2_HOSTKEY_TYPE GetRemoteHostKeyTyoe()
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

        //---------------------------------------------------------------------
        // User auth.
        //---------------------------------------------------------------------

        public bool IsAuthenticated
        {
            get
            {
                return UnsafeNativeMethods.libssh2_userauth_authenticated(
                    this.session.Handle) == 1;
            }
        }

        public string[] GetAuthenticationMethods(string username)
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

        public void Authenticate(
            string username,
            string pemPublicKey)
        {
            int Sign(
                SshSessionHandle session,
                out IntPtr signature,
                out IntPtr signatureLength,
                IntPtr data,
                IntPtr dataLength,
                IntPtr context)
            {
                Debug.Assert(context == IntPtr.Zero);
                Debug.Assert(session.DangerousGetHandle() == 
                    this.session.Handle.DangerousGetHandle());

                signature = IntPtr.Zero;
                signatureLength = IntPtr.Zero;

                // NB. libssh2 frees the data using the allocator passed in
                // libssh2_session_init_ex.

                return (int)LIBSSH2_ERROR.NONE;
            }


            // TODO: Format public key as ssh-rda/ssh-dsa,
            // see https://github.com/stuntbadger/GuacamoleServer/blob/a06ae0743b0609cde0ceccc7ed136b0d71009105/src/common-ssh/key.c#L86

            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_userauth_publickey(
                this.session.Handle,
                username,
                pemPublicKey,
                new IntPtr(pemPublicKey.Length),
                Sign,
                IntPtr.Zero);
            if (result != LIBSSH2_ERROR.NONE)
            {
                throw new SshNativeException(result);
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
                this.socket.Dispose();
            }
        }
    }
}
