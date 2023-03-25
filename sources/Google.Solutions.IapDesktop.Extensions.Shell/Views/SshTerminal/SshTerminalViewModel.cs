﻿//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.Common.Util;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Adapter;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.Download;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    [Service]
    public class SshTerminalViewModel : TerminalViewModelBase, ISshAuthenticator, ITextTerminal
    {
        private RemoteShellChannel sshChannel = null;

        private readonly IConfirmationDialog confirmationDialog;
        private readonly IOperationProgressDialog operationProgressDialog;
        private readonly IDownloadFileDialog downloadFileDialog;
        private readonly IExceptionDialog exceptionDialog;
        private readonly IQuarantineAdapter quarantineAdapter;
        private readonly IJobService jobService;

        public event EventHandler<AuthenticationPromptEventArgs> AuthenticationPrompt;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalViewModel(
            IEventService eventService,
            IJobService jobService,
            IConfirmationDialog confirmationDialog,
            IOperationProgressDialog operationProgressDialog,
            IDownloadFileDialog downloadFileDialog,
            IExceptionDialog exceptionDialog,
            IQuarantineAdapter quarantineAdapter)
            : base(eventService)
        {
            this.jobService = jobService;

            this.confirmationDialog = confirmationDialog;
            this.operationProgressDialog = operationProgressDialog;
            this.downloadFileDialog = downloadFileDialog;
            this.exceptionDialog = exceptionDialog;
            this.quarantineAdapter = quarantineAdapter;
        }

        //---------------------------------------------------------------------
        // Initialization properties.
        //---------------------------------------------------------------------

        internal CultureInfo Language { get; set; }
        internal IPEndPoint Endpoint { get; set; }
        internal AuthorizedKeyPair AuthorizedKey { get; set; }
        internal TimeSpan ConnectionTimeout { get; set; }

        protected override void OnValidate()
        {
            base.OnValidate();

            this.Endpoint.ExpectNotNull(nameof(this.Endpoint));
            this.AuthorizedKey.ExpectNotNull(nameof(this.AuthorizedKey));
            this.ConnectionTimeout.ExpectNotNull(nameof(this.ConnectionTimeout));
        }

        //---------------------------------------------------------------------
        // ISshAuthenticator.
        //---------------------------------------------------------------------

        string ISshAuthenticator.Username => this.AuthorizedKey.Username;

        ISshKeyPair ISshAuthenticator.KeyPair => this.AuthorizedKey.KeyPair;

        string ISshAuthenticator.Prompt(
            string name,
            string instruction,
            string prompt,
            bool echo)
        {
            Debug.Assert(this.View != null, "Not disposed yet");
            Debug.Assert(!this.ViewInvoker.InvokeRequired, "On UI thread");

            var args = new AuthenticationPromptEventArgs(prompt, !echo);
            this.AuthenticationPrompt?.Invoke(this, args);

            if (args.Response != null)
            {
                //
                // Strip:
                //  - spaces between group of digits (g.co/sc)
                //  - "G-" prefix (text messages)
                //
                if (args.Response.StartsWith("g-", StringComparison.OrdinalIgnoreCase))
                {
                    args.Response = args.Response.Substring(2);
                }

                return args.Response.Replace(" ", string.Empty);
            }
            else
            {
                return null;
            }
        }

        //---------------------------------------------------------------------
        // ITextTerminal.
        //---------------------------------------------------------------------

        string ITextTerminal.TerminalType => RemoteShellChannel.DefaultTerminal;

        CultureInfo ITextTerminal.Locale => this.Language;

        void ITextTerminal.OnDataReceived(string data)
        {
            if (this.View == null)
            {
                //
                // The window has already been closed/disposed, but there's
                // still data arriving. Ignore.
                //
                return;
            }

            Debug.Assert(!this.ViewInvoker.InvokeRequired, "On UI thread");

            OnDataReceived(new DataEventArgs(data));
        }

        void ITextTerminal.OnError(TerminalErrorType errorType, Exception exception)
        {
            if (this.View == null)
            {
                //
                // The window has already been closed/disposed, but there's
                // still data arriving. Ignore.
                //
                return;
            }

            Debug.Assert(!this.ViewInvoker.InvokeRequired, "On UI thread");

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(exception))
            {
                if (this.ConnectionStatus == Status.Connected &&
                    errorType == TerminalErrorType.ConnectionLost)
                {
                    OnConnectionLost(new ConnectionErrorEventArgs(exception))
                        .ContinueWith(_ => { });
                }
                else
                {
                    OnConnectionFailed(new ConnectionErrorEventArgs(exception))
                        .ContinueWith(_ => { });
                }
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private async Task<RemoteShellChannel> ConnectAndTranslateErrorsAsync(
            TerminalSize initialSize)
        {
            try
            {
                var connection = new RemoteConnection(
                        this.Endpoint,
                        (ISshAuthenticator)this,
                        SynchronizationContext.Current)
                {
                    Banner = SshSession.BannerPrefix + Install.UserAgent,
                    ConnectionTimeout = this.ConnectionTimeout,

                    //
                    // NB. Do not join worker thread as this could block the
                    // UI thread.
                    //
                    JoinWorkerThreadOnDispose = false
                };

                await connection.ConnectAsync()
                    .ConfigureAwait(false);

                return await connection.OpenShellAsync(
                        (ITextTerminal)this,
                        initialSize)
                    .ConfigureAwait(false);
            }
            catch (SshNativeException e) when (
                e.ErrorCode == LIBSSH2_ERROR.AUTHENTICATION_FAILED)
            {
                var keyPairWarning = string.Empty;
                if (this.AuthorizedKey.KeyPair is RsaSshKeyPair)
                {
                    keyPairWarning =
                        "Some Linux distributions also no longer support the " +
                        $"'{this.AuthorizedKey.KeyPair.Type}' algorithm that you're " +
                        "currently using for SSH authentication. To use a more modern " +
                        "algorithm, go to Tools > Options > SSH and " +
                        "configure IAP Desktop to use ECDSA instead of RSA.";
                }

                if (this.AuthorizedKey.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
                {
                    throw new OsLoginAuthenticationFailedException(
                        "You do not have sufficient permissions to access this VM instance.\n\n" +
                        "To perform this action, you need the following roles (or an equivalent custom role):\n\n" +
                        " 1. 'Compute OS Login' or 'Compute OS Admin Login'\n" +
                        " 2. 'Service Account User' (if the VM uses a service account)\n" +
                        " 3. 'Compute OS Login External User' (if the VM belongs to a different GCP organization)\n\n" +
                        keyPairWarning,
                        e,
                        HelpTopics.GrantingOsLoginRoles);
                }
                else
                {
                    throw new MetadataKeyAuthenticationFailedException(
                        "Authentication failed. Verify that the Compute Engine guest environment " +
                        "is installed on the VM and that the agent is running.\n\n" +
                        keyPairWarning,
                        e,
                        HelpTopics.ManagingMetadataAuthorizedKeys);
                }
            }
        }

        public override async Task ConnectAsync(TerminalSize initialSize)
        {
            Debug.Assert(this.View != null);
            Debug.Assert(this.ViewInvoker != null);

            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                //
                // Disconnect previous session, if any.
                //
                await DisconnectAsync()
                    .ConfigureAwait(true);
                Debug.Assert(this.sshChannel == null);

                //
                // Establish a new connection and create a shell.
                //
                try
                {
                    //
                    // Force all callbacks to run on the current
                    // synchronization context (i.e., the UI thread.
                    //
                    await OnBeginConnect().ConfigureAwait(true);

                    this.sshChannel = await ConnectAndTranslateErrorsAsync(
                            initialSize)
                        .ConfigureAwait(true);

                    await OnConnected().ConfigureAwait(true);
                }
                catch (Exception e)
                {
                    ApplicationTraceSources.Default.TraceError(e);

                    await OnConnectionFailed(new ConnectionErrorEventArgs(e))
                        .ConfigureAwait(true);

                    this.sshChannel = null;
                }
            }
        }

        public override async Task DisconnectAsync()
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            {
                if (this.sshChannel != null)
                {
                    this.sshChannel.Connection.Dispose();
                    this.sshChannel = null;

                    await OnDisconnected().ConfigureAwait(true);
                }
            }
        }

        public override async Task SendAsync(string command)
        {
            if (this.sshChannel != null)
            {
                await this.sshChannel.SendAsync(command)
                    .ConfigureAwait(false);

                OnDataSent(new DataEventArgs(command));
            }
        }


        public override async Task ResizeTerminal(TerminalSize newSize)
        {
            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(newSize))
            {
                if (this.sshChannel != null)
                {
                    await this.sshChannel.ResizeTerminalAsync(newSize)
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> DownloadFilesAsync()
        {
            if (this.sshChannel == null)
            {
                return false;
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithoutParameters())
            using (var fsChannel = await this.sshChannel.Connection
                .OpenFileSystemAsync()
                .ConfigureAwait(true))
            using (var fs = new SftpFileSystem(fsChannel))
            {
                if (this.downloadFileDialog.SelectDownloadFiles(
                    this.View,
                    "Select files to download",
                    fs,
                    out var selectedFiles,
                    out var targetDirectory) != DialogResult.OK ||
                    !targetDirectory.Exists)
                {
                    return false;
                }

                //
                // Check if any of the files exist already.
                //
                var existingFileNames = targetDirectory
                    .GetFiles()
                    .Select(f => f.Name)
                    .ToHashSet();

                var conflicts = existingFileNames
                    .Intersect(selectedFiles.Select(i => i.Name));

                if (conflicts.Any())
                {
                    var message =
                        $"The following files already exist in {targetDirectory.FullName}.\n\n - " +
                        string.Join("\n - ", conflicts.Select(s => s.Truncate(30))) +
                        $"\n\nDo you want to overwrite existing files?";

                    if (this.confirmationDialog.Confirm(
                        this.View,
                        message,
                        $"Download files from {this.Instance.Name}",
                        "Download files") != DialogResult.Yes)
                    {
                        return false;
                    }
                }

                //
                // Download files non-recursively in a background job. For better UX, wrap
                // the background job using the Shell progress dialog.
                //
                // NB. The size calculation doesn't work for links, but we're not allowing
                // links to be selected in the dialog.
                //
                Debug.Assert(!selectedFiles.Any(i => i.Attributes.HasFlag(FileAttributes.ReparsePoint)));
                Debug.Assert(!selectedFiles.Any(i => i.Attributes.HasFlag(FileAttributes.Directory)));

                try
                {
                    using (var progressDialog = this.operationProgressDialog.ShowCopyDialog(
                        this.View,
                        (ulong)selectedFiles.Count(),
                        (ulong)selectedFiles.Sum(f => (long)f.Size)))
                    {
                        return await this.jobService.RunInBackground<bool>(
                            new JobDescription(
                                $"Downloading files from {this.Instance.Name}",
                                JobUserFeedbackType.BackgroundFeedback),
                            async _ =>
                            {
                                //
                                // Perform downloads sequentally as we're only using a single
                                // SSH connection anyway.
                                //
                                var quarantineTasks = new List<Task>();
                                foreach (var file in selectedFiles)
                                {
                                    //
                                    // NB. The remote file name might not be Win32-compliant,
                                    // so we need to escape it.
                                    //
                                    var targetFile = new FileInfo(Path.Combine(
                                        targetDirectory.FullName,
                                        WindowsFilename.EscapeFilename(file.Name)));
                                    using (var fileStream = targetFile.OpenWrite())
                                    {
                                        await fsChannel.DownloadFileAsync(
                                                file.Path,
                                                fileStream,
                                                new Progress<uint>(delta => progressDialog.OnBytesCompleted(delta)),
                                                progressDialog.CancellationToken)
                                            .ConfigureAwait(true);
                                    }

                                    //
                                    // Scan for malware and apply MOTW.
                                    //
                                    // Don't block downloads while scanning take place, and don't
                                    // abort pending downloads when scanning fails.
                                    //
                                    quarantineTasks.Add(this.quarantineAdapter.ScanAsync(
                                        IntPtr.Zero, // Can't use window handle on this thread.
                                        targetFile));

                                    progressDialog.OnItemCompleted();
                                }

                                //
                                // Propagate exception if a file has been qurantined so
                                // that we can handle it when we're back on the UI thread.
                                //
                                await Task.WhenAll(quarantineTasks.ToArray());

                                return true;
                            })
                            .ConfigureAwait(true);
                    }
                }
                catch (Exception e) when (e.Is<QuarantineException>())
                {
                    this.exceptionDialog.Show(this.View, "Download blocked", e);
                    return false;
                }
            }
        }

        public async Task<bool> UploadFilesAsync(IEnumerable<FileInfo> files)
        {
            if (this.sshChannel == null)
            {
                return false;
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(files))
            using (var fsChannel = await this.sshChannel.Connection
                .OpenFileSystemAsync()
                .ConfigureAwait(true))
            {
                //
                // Check if any of the files exist already.
                //
                var allFilesInHomeDir = await fsChannel
                    .ListFilesAsync(".")
                    .ConfigureAwait(true);

                var existingFileNames = allFilesInHomeDir
                    .Where(f => !f.IsDirectory)
                    .Select(f => f.Name)
                    .ToHashSet();

                var fileNamesToUpload = files
                    .Select(f => f.Name);

                var conflicts = existingFileNames.Intersect(fileNamesToUpload);

                var message = "Are you sure you want to upload the following " +
                    $"file(s) to your home directory on {this.Instance.Name}?\n\n - " +
                    string.Join("\n - ", fileNamesToUpload.Select(s => s.Truncate(30)));
                if (conflicts.Any())
                {
                    message +=
                        "\n\nThe following files already exist on the server and will be replaced:\n\n - " +
                        string.Join("\n - ", conflicts.Select(s => s.Truncate(30)));
                }

                if (this.confirmationDialog.Confirm(
                    this.View,
                    message,
                    $"Upload file(s) to {this.Instance.Name}",
                    "Upload file") != DialogResult.Yes)
                {
                    return false;
                }

                //
                // Upload files in a background job. For better UX, wrap
                // the background job using the Shell progress dialog.
                //
                using (var progressDialog = this.operationProgressDialog.ShowCopyDialog(
                    this.View,
                    (ulong)files.Count(),
                    (ulong)files.Sum(f => f.Length)))
                {
                    return await this.jobService.RunInBackground<bool>(
                        new JobDescription(
                            $"Uploading files to {this.Instance.Name}",
                            JobUserFeedbackType.BackgroundFeedback),
                        async _ =>
                        {
                            foreach (var file in files)
                            {
                                using (var fileStream = file.OpenRead())
                                {
                                    await fsChannel.UploadFileAsync(
                                            file.Name,  // Relative path -> place in home directory
                                            fileStream,
                                            LIBSSH2_FXF_FLAGS.TRUNC | LIBSSH2_FXF_FLAGS.CREAT | LIBSSH2_FXF_FLAGS.WRITE,
                                            FilePermissions.OwnerRead | FilePermissions.OwnerWrite,
                                            new Progress<uint>(delta => progressDialog.OnBytesCompleted(delta)),
                                            progressDialog.CancellationToken)
                                        .ConfigureAwait(false);
                                }

                                progressDialog.OnItemCompleted();
                            }

                            return true;
                        })
                        .ConfigureAwait(true);
                }
            }
        }

        public static IEnumerable<FileInfo> GetDroppableFiles(object dropData)
        {
            if (dropData is IEnumerable<string> filePaths)
            {
                //
                // Only allow dropping files, ignore directories.
                //
                return filePaths
                    .Where(f => File.Exists(f))
                    .Where(f => !File.GetAttributes(f).HasFlag(FileAttributes.Directory))
                    .Select(f => new FileInfo(f))
                    .ToList();
            }
            else
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.sshChannel?.Connection.Dispose();
                this.AuthorizedKey.Dispose();
            }
        }
    }

    public class AuthenticationPromptEventArgs
    {
        public bool IsPasswordPrompt { get; }
        public string Prompt { get; }
        public string Response { get; set; }

        public AuthenticationPromptEventArgs(
            string prompt,
            bool isPasswordPrompt)
        {
            this.IsPasswordPrompt = isPasswordPrompt;
            this.Prompt = prompt;
        }
    }

    public class OsLoginAuthenticationFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public OsLoginAuthenticationFailedException(
            string message,
            Exception inner,
            IHelpTopic helpTopic)
            : base(message, inner)
        {
            this.Help = helpTopic;
        }
    }

    public class MetadataKeyAuthenticationFailedException : Exception, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public MetadataKeyAuthenticationFailedException(
            string message,
            Exception inner,
            IHelpTopic helpTopic)
            : base(message, inner)
        {
            this.Help = helpTopic;
        }
    }

    public class DownloadBlockedException : Exception
    {
        public DownloadBlockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
