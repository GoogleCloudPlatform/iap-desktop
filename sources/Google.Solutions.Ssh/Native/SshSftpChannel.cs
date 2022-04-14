using Google.Solutions.Common.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    public class SshSftpChannel : IDisposable
    {
        private const uint MaxFilenameLength = 256;

        private readonly SshSession session;
        private readonly SshSftpChannelHandle channelHandle;

        private bool disposed = false;

        internal SshSftpChannel(
            SshSession session,
            SshSftpChannelHandle channelHandle)
        {
            this.session = session;
            this.channelHandle = channelHandle;
        }

        //---------------------------------------------------------------------

        public IReadOnlyCollection<SshSftpFile> ListFiles(string path)
        {
            var files = new LinkedList<SshSftpFile>();

            using (SshTraceSources.Default.TraceMethod().WithParameters(path))
            using (var dirHandle = UnsafeNativeMethods.libssh2_sftp_open_ex(
                this.channelHandle,
                path,
                (uint)path.Length,
                0,
                0,
                LIBSSH2_OPENTYPE.OPENDIR))
            {
                try
                {
                    dirHandle.ValidateAndAttachToSession(this.session);

                    //
                    // NB. longEntry is a human-readable listing as produced by `ls`
                    // and isn't useful for us.
                    //
                    using (var fileNameBuffer = GlobalAllocSafeHandle.GlobalAlloc(MaxFilenameLength))
                    using (var longEntryBuffer = GlobalAllocSafeHandle.GlobalAlloc(MaxFilenameLength))
                    {
                        while (true)
                        {
                            var bytesInBuffer = UnsafeNativeMethods.libssh2_sftp_readdir_ex(
                                dirHandle,
                                fileNameBuffer.DangerousGetHandle(),
                                new IntPtr(MaxFilenameLength),
                                longEntryBuffer.DangerousGetHandle(),
                                new IntPtr(MaxFilenameLength),
                                out var attributes);
                            if (bytesInBuffer == 0)
                            {
                                //
                                // End of list reached.
                                //
                                return files;
                            }
                            else if (bytesInBuffer < 0)
                            {
                                throw this.session.CreateException((LIBSSH2_ERROR)bytesInBuffer);
                            }
                            else
                            {
                                files.AddLast(new SshSftpFile(
                                    Marshal.PtrToStringAnsi(fileNameBuffer.DangerousGetHandle()),
                                    attributes));
                            }
                        }
                    }
                }
                catch (SshNativeException e) when (e.ErrorCode == LIBSSH2_ERROR.SFTP_PROTOCOL)
                {
                    throw SshSftpNativeException.GetLastError(
                        this.channelHandle,
                        path);
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
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }

    public struct SshSftpFile
    {
        /// <summary>
        /// Name of file (without path).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// File attributes.
        /// </summary>
        public LIBSSH2_SFTP_ATTRIBUTES Attributes { get; }

        public FilePermissions Permissions 
            => (FilePermissions)this.Attributes.permissions;

        public bool IsDirectory 
            => this.Permissions.HasFlag(FilePermissions.Directory);

        public SshSftpFile(
            string name,
            LIBSSH2_SFTP_ATTRIBUTES attributes)
        {
            this.Name = name;
            this.Attributes = attributes;
        }
    }

    public class SshSftpNativeException : SshException
    {
        public int Errno { get; }

        private SshSftpNativeException(
            int errno,
            string message)
            : base(message)
        {
            this.Errno = errno;
        }

        internal static SshSftpNativeException GetLastError(
            SshSftpChannelHandle channelHandle,
            string path)
        {
            var errno = UnsafeNativeMethods.libssh2_sftp_last_error(
                channelHandle);
            return new SshSftpNativeException(
                errno,
                $"{path}: SFTP operation failed: {errno}");
        }
    }
}
