using Google.Apis.Util;
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
            Utilities.ThrowIfNullOrEmpty(path, nameof(path));

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

        public void CreateDirectory(
            string path,
            FilePermissions filePermissions)
        {
            Utilities.ThrowIfNullOrEmpty(path, nameof(path));

            using (SshTraceSources.Default.TraceMethod().WithParameters(path))
            {
                try
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_sftp_mkdir_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length,
                        filePermissions);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException(result);
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

        public void DeleteDirectory(string path)
        {
            Utilities.ThrowIfNullOrEmpty(path, nameof(path));

            using (SshTraceSources.Default.TraceMethod().WithParameters(path))
            {
                try
                {
                    var result = (LIBSSH2_ERROR)UnsafeNativeMethods.libssh2_sftp_rmdir_ex(
                        this.channelHandle,
                        path,
                        (uint)path.Length);

                    if (result != LIBSSH2_ERROR.NONE)
                    {
                        throw this.session.CreateException(result);
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
        private readonly LIBSSH2_SFTP_ATTRIBUTES attributes;

        /// <summary>
        /// Name of file (without path).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// File attributes.
        /// </summary>
        
        public FilePermissions Permissions 
            => (FilePermissions)this.attributes.permissions;

        public bool IsDirectory 
            => this.Permissions.HasFlag(FilePermissions.Directory);

        // TODO: expose file times, etc.

        internal SshSftpFile(
            string name,
            LIBSSH2_SFTP_ATTRIBUTES attributes)
        {
            this.Name = name;
            this.attributes = attributes;
        }
    }
}
