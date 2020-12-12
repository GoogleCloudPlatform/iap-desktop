using System;
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

            if (keyPtr == IntPtr.Zero || keyLength <= 0)
            {
                return null;
            }
            else
            {
                var key = new byte[keyLength];
                Marshal.Copy(keyPtr, key, 0, keyLength);
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
