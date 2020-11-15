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
                UnsafeNativeMethods.Call(
                    () => UnsafeNativeMethods.libssh2_init(0));
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
        // Publics.
        //---------------------------------------------------------------------

        public bool Blocking
        {
            get
            {
                return UnsafeNativeMethods.libssh2_session_get_blocking(
                    this.sessionHandle) != 0;
            }
            set
            {
                UnsafeNativeMethods.libssh2_session_set_blocking(
                    this.sessionHandle,
                    value ? 1 : 0);
            }
        }

        public string[] GetSupportedAlgorithms(LIBSSH2_METHOD methodType)
        {
            int count = UnsafeNativeMethods.libssh2_session_supported_algs(
                this.sessionHandle,
                methodType,
                out IntPtr algorithmsPtrPtr);
            if (count == 0 || algorithmsPtrPtr == IntPtr.Zero)
            {
                return Array.Empty<string>();
            }

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

            UnsafeNativeMethods.Call(
                () => UnsafeNativeMethods.libssh2_session_method_pref(
                    this.sessionHandle,
                    methodType,
                    prefs));
        }




        public void Handshake(Socket socket)
        {
            UnsafeNativeMethods.Call(
                () => UnsafeNativeMethods.libssh2_session_handshake(
                    this.sessionHandle,
                    socket.Handle));
        }

        public byte[] GetHostKeyHash(LIBSSH2_HOSTKEY_HASH hashType)
        {
            var hashPtr = UnsafeNativeMethods.libssh2_hostkey_hash(
                this.sessionHandle, 
                hashType);

            var hash = new byte[HostKeyHashLength(hashType)];
            Marshal.Copy(hashPtr, hash, 0, hash.Length);
            return hash;
        }

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
            UnsafeNativeMethods.Call(
                () => UnsafeNativeMethods.libssh2_session_disconnect_ex(
                    this.sessionHandle,
                    0,
                    null,
                    null));

            this.sessionHandle.Dispose();
        }
    }
}
