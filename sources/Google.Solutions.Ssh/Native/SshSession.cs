using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// Wrapper for native libssh functions.
    /// All methods are potentially blocking.
    /// </summary>
    public sealed class SshSession : IDisposable
    {
        private readonly SshSessionHandle sessionHandle;

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

        static SshSession()
        {
            try
            {

                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_init(0);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    throw new SshNativeException(result);
                }
            }
            catch (EntryPointNotFoundException)
            {
                throw new SshException("libssh2 DLL not found or could not be loaded");
            }
        }

        public SshSession()
        {
            this.sessionHandle = UnsafeNativeMethods.libssh2_session_init_ex(
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        public string[] GetSupportedAlgorithms(LIBSSH2_METHOD methodType)
        {
            int count = UnsafeNativeMethods.libssh2_session_supported_algs(
                this.sessionHandle,
                methodType,
                out IntPtr algorithmsPtrPtr);
            if (count > 0 && algorithmsPtrPtr != IntPtr.Zero)
            {
                var algorithmsPtrs = new IntPtr[count];
                Marshal.Copy(algorithmsPtrPtr, algorithmsPtrs, 0, algorithmsPtrs.Length);

                var algorithms = algorithmsPtrs
                    .Select(ptr => Marshal.PtrToStringAnsi(ptr))
                    .ToArray();

                UnsafeNativeMethods.libssh2_free(
                    this.sessionHandle,
                    algorithmsPtrPtr);

                return algorithms;
            }
            else if (count < 0)
            {
                throw new SshNativeException((LIBSSH2_ERROR)count);
            }
            else
            {
                return Array.Empty<string>();
            }
        }

        public string[] GetActiveAlgorithms(LIBSSH2_METHOD methodType)
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

        public void SetPreferredMethods(
            LIBSSH2_METHOD methodType,
            string[] methods)
        {
            var prefs = string.Join(",", methods);

            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_method_pref(
                this.sessionHandle,
                methodType,
                prefs);
            if (result != LIBSSH2_ERROR.NONE)
            {
                throw new SshNativeException(result);
            }
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public void SetLocalBanner(string banner)
        {
            UnsafeNativeMethods.libssh2_session_banner_set(
                   this.sessionHandle,
                   banner);
        }

        public string GetRemoteBanner()
        {
            var bannerPtr = UnsafeNativeMethods.libssh2_session_banner_get(
                this.sessionHandle);

            return bannerPtr == IntPtr.Zero
                ? null
                : Marshal.PtrToStringAnsi(bannerPtr);
        }

        //---------------------------------------------------------------------
        // Handshake.
        //---------------------------------------------------------------------

        public void Handshake(Socket socket)
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_handshake(
                this.sessionHandle,
                socket.Handle);
            if (result != LIBSSH2_ERROR.NONE)
            {
                throw new SshNativeException(result);
            }
        }

        public byte[] GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH hashType)
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

        public byte[] GetRemoteHostKey()
        {
            var keyPtr = UnsafeNativeMethods.libssh2_session_hostkey(
                this.sessionHandle,
                out var keyLength);

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

        //---------------------------------------------------------------------
        // Tracing.
        //---------------------------------------------------------------------

        public void SetTraceHandler(
            LIBSSH2_TRACE mask,
            Action<string> handler)
        {
            UnsafeNativeMethods.libssh2_trace_sethandler(
                this.sessionHandle,
                IntPtr.Zero,
                (sessionPtr, contextPtr, dataPtr, length) =>
                {
                    Debug.Assert(contextPtr == IntPtr.Zero);

                    var data = new byte[length];
                    Marshal.Copy(dataPtr, data, 0, length);

                    handler(Encoding.ASCII.GetString(data));
                });
            UnsafeNativeMethods.libssh2_trace(
                this.sessionHandle, 
                mask);
        }

        public void Dispose()
        {
            var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_disconnect_ex(
                this.sessionHandle,
                0,
                null,
                null);

            Debug.Assert(result == LIBSSH2_ERROR.NONE);

            this.sessionHandle.Dispose();
        }
    }
}
