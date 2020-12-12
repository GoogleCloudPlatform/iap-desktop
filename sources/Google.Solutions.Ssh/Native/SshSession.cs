using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An (unconnected) Libssh2 session.
    /// </summary>
    public class SshSession : IDisposable
    {
        private readonly SshSessionHandle sessionHandle;
        private bool disposed = false;

        internal SshSessionHandle Handle => this.sessionHandle;

        private static IntPtr Alloc(
            IntPtr size,
            IntPtr context)
        {
            return Marshal.AllocHGlobal(size);
        }

        private static IntPtr Realloc(
            IntPtr ptr,
            IntPtr size,
            IntPtr context)
        {
            return Marshal.ReAllocHGlobal(ptr, size);
        }

        private static void Free(
            IntPtr ptr,
            IntPtr context)
        {
            Marshal.FreeHGlobal(ptr);
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
                Alloc,
                Free,
                Realloc,
                IntPtr.Zero);
        }

        public static string GetVersion(Version requiredVersion)
        {
            var requiredVersionEncoded = 
                (requiredVersion.Major << 16) |
                (requiredVersion.Minor << 8) |
                (requiredVersion.Build);

            return Marshal.PtrToStringAnsi(
                UnsafeNativeMethods.libssh2_version(
                    requiredVersionEncoded));
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

        //---------------------------------------------------------------------
        // Timeout.
        //---------------------------------------------------------------------

        public TimeSpan Timeout
        {
            get
            {
                var millis = UnsafeNativeMethods.libssh2_session_get_timeout(
                    this.sessionHandle);
                return TimeSpan.FromMilliseconds(millis);
            }
            set
            {
                UnsafeNativeMethods.libssh2_session_set_timeout(
                    this.sessionHandle,
                    (int)value.TotalMilliseconds);
            }
        }

        //---------------------------------------------------------------------
        // Handshake.
        //---------------------------------------------------------------------

        public Task<SshConnection> ConnectAsync(EndPoint remoteEndpoint)
        {
            return Task.Run(() =>
            {
                var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                socket.Connect(remoteEndpoint);

                var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_session_handshake(
                    this.sessionHandle,
                    socket.Handle);
                if (result != LIBSSH2_ERROR.NONE)
                {
                    socket.Close();
                    throw new SshNativeException(result);
                }

                return new SshConnection(this, socket);
            });
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

                    var data = new byte[length.ToInt32()];
                    Marshal.Copy(dataPtr, data, 0, length.ToInt32());

                    handler(Encoding.ASCII.GetString(data));
                });
            UnsafeNativeMethods.libssh2_trace(
                this.sessionHandle, 
                mask);
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
}
