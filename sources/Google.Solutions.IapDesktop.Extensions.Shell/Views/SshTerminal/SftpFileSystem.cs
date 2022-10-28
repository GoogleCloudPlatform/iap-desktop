using Google.Apis.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    internal sealed class SftpFileSystem : FileBrowser.IFileSystem, IDisposable // TODO: Test
    {
        private readonly RemoteFileSystemChannel channel;
        private readonly FileTypeCache fileTypeCache;

        private FileType TranslateFileType(SshSftpFileInfo sftpFile)
        {
            if (sftpFile.IsDirectory)
            {
                return this.fileTypeCache.Lookup(
                    sftpFile.Name, 
                    FileAttributes.Directory, 
                    FileType.IconFlags.None);
            }
            else if (sftpFile.Permissions.HasFlag(FilePermissions.OwnerExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.GroupExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.OtherExecute))
            {
                //
                // Treat like an exe file.
                //
                return this.fileTypeCache.Lookup(
                    ".exe", 
                    FileAttributes.Normal, 
                    FileType.IconFlags.None);
            }
            else 
            {
                return this.fileTypeCache.Lookup(
                    sftpFile.Name, 
                    FileAttributes.Normal, 
                    FileType.IconFlags.None);
            }
            // TODO: Map config -> INI
            // TODO: Map symlinks
        }

        public SftpFileSystem(RemoteFileSystemChannel channel)
        {
            this.channel = channel.ThrowIfNull(nameof(channel));
            this.fileTypeCache = new FileTypeCache();

            this.Root = new SftpRootItem();
        }

        //---------------------------------------------------------------------
        // IFileSystem.
        //---------------------------------------------------------------------

        public FileBrowser.IFileItem Root { get; }

        public async Task<ObservableCollection<FileBrowser.IFileItem>> ListFilesAsync(
            FileBrowser.IFileItem directory)
        {
            var sftpDirectory = (ISftpFileItem)directory;
            Debug.Assert(!sftpDirectory.Type.IsFile);

            var sftpFiles = await this.channel
                .ListFilesAsync(sftpDirectory.RemotePath)
                .ConfigureAwait(false);

            var fileItems = new ObservableCollection<FileBrowser.IFileItem>();

            foreach (var sftpFile in sftpFiles)
            {
                if (sftpFile.Name == "." || sftpFile.Name == "..")
                {
                    continue;
                }

                fileItems.Add(new SftpFileItem(
                    sftpDirectory, 
                    sftpFile, 
                    TranslateFileType(sftpFile)));
            }

            return fileItems;
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.fileTypeCache.Dispose();
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private interface ISftpFileItem : FileBrowser.IFileItem
        {
            string RemotePath { get; }
        }

        private class SftpRootItem : ISftpFileItem
        {
            public string Name => string.Empty; // TODO: Verify impact on FileBrowser.CurrentPath

            public FileAttributes Attributes => FileAttributes.Directory;

            public DateTime LastModified => DateTimeOffset.FromUnixTimeSeconds(0).DateTime;

            public ulong Size => 0;

            public FileType Type => new FileType(
                "Server",
                false,
                SystemIcons.WinLogo.ToBitmap()); // TODO: Use static image for VM

            public bool IsExpanded { get; set; } = true;

            public string RemotePath => "/";

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private class SftpFileItem : ISftpFileItem
        {
            private readonly ISftpFileItem parent;
            private readonly SshSftpFileInfo fileInfo;

            public SftpFileItem(
                ISftpFileItem parent,
                SshSftpFileInfo fileInfo, 
                FileType type)
            {
                this.parent = parent;
                this.fileInfo = fileInfo;
                this.Type = type;
            }

            public string RemotePath
                => (this.parent?.RemotePath ?? string.Empty) + "/" + this.Name;

            public string Name => this.fileInfo.Name;

            public DateTime LastModified => this.fileInfo.LastModifiedDate;

            public ulong Size => this.fileInfo.Size;

            public bool IsExpanded { get; set; }

            public FileType Type { get; }

            public FileAttributes Attributes
            {
                get
                {
                    var attributes = FileAttributes.Normal;

                    if (this.fileInfo.IsDirectory)
                    {
                        attributes |= FileAttributes.Directory;
                    }

                    if (this.fileInfo.Name.StartsWith("."))
                    {
                        attributes |= FileAttributes.Hidden;
                    }

                    // TODO: Device?

                    return attributes;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
