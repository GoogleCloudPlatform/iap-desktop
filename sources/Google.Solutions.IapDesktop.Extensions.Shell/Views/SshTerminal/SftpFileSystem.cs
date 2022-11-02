using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    internal sealed class SftpFileSystem : FileBrowser.IFileSystem, IDisposable
    {
        private readonly Func<string, Task<IReadOnlyCollection<SshSftpFileInfo>>> listRemoteFilesFunc;
        private readonly FileTypeCache fileTypeCache;

        private static Regex configFileNamePattern = new Regex("co?ni?f(ig)?$");

        internal FileType TranslateFileType(SshSftpFileInfo sftpFile)
        {
            if (sftpFile.IsDirectory)
            {
                return this.fileTypeCache.Lookup(
                    sftpFile.Name, 
                    FileAttributes.Directory, 
                    FileType.IconFlags.None);
            }
            else if (sftpFile.Permissions.HasFlag(FilePermissions.SymbolicLink))
            {
                //
                // Treat like an LNK file.
                //
                // NB. We can't tell whether the symlink points to a directory
                // or not, that would require resolving the link. So we treat
                // all symlinks like files.
                //
                return this.fileTypeCache.Lookup(
                    ".lnk",
                    FileAttributes.Normal,
                    FileType.IconFlags.None);
            }
            else if (sftpFile.Permissions.HasFlag(FilePermissions.OwnerExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.GroupExecute) ||
                     sftpFile.Permissions.HasFlag(FilePermissions.OtherExecute))
            {
                //
                // Treat like an EXE file.
                //
                return this.fileTypeCache.Lookup(
                    ".exe", 
                    FileAttributes.Normal, 
                    FileType.IconFlags.None);
            }
            else if (configFileNamePattern.IsMatch(sftpFile.Name))
            {
                //
                // Treat like an INI file.
                //
                return this.fileTypeCache.Lookup(
                    ".ini",
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
        }
        
        internal SftpFileSystem(
            Func<string, Task<IReadOnlyCollection<SshSftpFileInfo>>> listRemoteFilesFunc)
        {
            this.listRemoteFilesFunc = listRemoteFilesFunc;
            this.fileTypeCache = new FileTypeCache();

            this.Root = new SftpRootItem();
        }

        public SftpFileSystem(RemoteFileSystemChannel channel)
            : this((path) => channel.ListFilesAsync(path))
        {
            channel.ThrowIfNull(nameof(channel));
        }

        //---------------------------------------------------------------------
        // IFileSystem.
        //---------------------------------------------------------------------

        public FileBrowser.IFileItem Root { get; }

        public async Task<ObservableCollection<FileBrowser.IFileItem>> ListFilesAsync(
            FileBrowser.IFileItem directory)
        {
            directory.ThrowIfNull(nameof(directory));
            Debug.Assert(!directory.Type.IsFile);

            var remotePath = directory == this.Root
                ? "/"
                : directory.Path;
            Debug.Assert(!remotePath.StartsWith("//"));

            var sftpFiles = await this
                .listRemoteFilesFunc(remotePath)
                .ConfigureAwait(false);

            //
            // NB. SFTP returns files/directories in arbitrary order.
            //

            var filteredSftpFiles = sftpFiles
                .Where(f => f.Name != "." && f.Name != "..")
                .OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name)
                .Select(f => new SftpFileItem(
                    directory,
                    f,
                    TranslateFileType(f)))
                .ToList();

            return new ObservableCollection<FileBrowser.IFileItem>(filteredSftpFiles);
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

        private class SftpRootItem : FileBrowser.IFileItem
        {
            public string Name => string.Empty;

            public FileAttributes Attributes => FileAttributes.Directory;

            public DateTime LastModified => DateTimeOffset.FromUnixTimeSeconds(0).DateTime;

            public ulong Size => 0;

            public FileType Type => new FileType(
                "Server",
                false,
                StockIcons.GetIcon(StockIcons.IconId.Server, StockIcons.IconSize.Small));

            public bool IsExpanded { get; set; } = true;

            public string Path => string.Empty;

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private class SftpFileItem : FileBrowser.IFileItem
        {
            private readonly FileBrowser.IFileItem parent;
            private readonly SshSftpFileInfo fileInfo;

            public SftpFileItem(
                FileBrowser.IFileItem parent,
                SshSftpFileInfo fileInfo, 
                FileType type)
            {
                this.parent = parent;
                this.fileInfo = fileInfo;
                this.Type = type;
            }

            public string Path
                => (this.parent?.Path ?? string.Empty) + "/" + this.Name;

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

                    if (this.fileInfo.Permissions.IsLink())
                    {
                        attributes |= FileAttributes.ReparsePoint;
                    }

                    if (this.fileInfo.Permissions.IsSocket() ||
                        this.fileInfo.Permissions.IsFifo() ||
                        this.fileInfo.Permissions.IsCharacterDevice() ||
                        this.fileInfo.Permissions.IsBlockDevice())
                    {
                        attributes |= FileAttributes.Device;
                    }

                    return attributes;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
