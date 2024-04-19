//
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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Protocol.Ssh;
using Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Download;
using Google.Solutions.Platform.Interop;
using Google.Solutions.Platform.Security;
using Google.Solutions.Ssh;
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
#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.ToolWindows.Ssh
{
    [Service]
    public class SshTerminalViewModel
        : TerminalViewModelBase, IKeyboardInteractiveHandler, ITextTerminal
    {
        private SshShellChannel sshChannel = null;

        private readonly IConfirmationDialog confirmationDialog;
        private readonly IOperationProgressDialog operationProgressDialog;
        private readonly IDownloadFileDialog downloadFileDialog;
        private readonly IExceptionDialog exceptionDialog;
        private readonly IQuarantine quarantine;
        private readonly IJobService jobService;

        public event EventHandler<AuthenticationPromptEventArgs> AuthenticationPrompt;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalViewModel(
            IEventQueue eventService,
            IJobService jobService,
            IConfirmationDialog confirmationDialog,
            IOperationProgressDialog operationProgressDialog,
            IDownloadFileDialog downloadFileDialog,
            IExceptionDialog exceptionDialog,
            IQuarantine quarantineAdapter)
            : base(eventService)
        {
            this.jobService = jobService;

            this.confirmationDialog = confirmationDialog;
            this.operationProgressDialog = operationProgressDialog;
            this.downloadFileDialog = downloadFileDialog;
            this.exceptionDialog = exceptionDialog;
            this.quarantine = quarantineAdapter;
        }

        //---------------------------------------------------------------------
        // Initialization properties.
        //---------------------------------------------------------------------

        internal CultureInfo Language { get; set; }
        internal IPEndPoint Endpoint { get; set; }
        internal ISshCredential Credential { get; set; }
        internal TimeSpan ConnectionTimeout { get; set; }

        protected override void OnValidate()
        {
            base.OnValidate();

            this.Endpoint.ExpectNotNull(nameof(this.Endpoint));
            this.Credential.ExpectNotNull(nameof(this.Credential));
        }

        //---------------------------------------------------------------------
        // IKeyboardInteractiveHandler.
        //---------------------------------------------------------------------

        string IKeyboardInteractiveHandler.Prompt(
            string caption,
            string instruction,
            string prompt,
            bool echo)
        {
            Debug.Assert(this.View != null, "Not disposed yet");
            Debug.Assert(!this.ViewInvoker.InvokeRequired, "On UI thread");

            var args = new AuthenticationPromptEventArgs(caption, prompt, !echo);
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

        IPasswordCredential IKeyboardInteractiveHandler.PromptForCredentials(
            string username)
        {
            Debug.Assert(this.View != null, "Not disposed yet");
            Debug.Assert(!this.ViewInvoker.InvokeRequired, "On UI thread");

            //
            // NB. We don't allow the username to be changed,
            // so using a CredUI prompt wouldn't add much value
            // over using a generic prompt.
            //

            var args = new AuthenticationPromptEventArgs(
                "Enter password for " + username,
                "These credentials will be used to connect to " + this.Instance.Name,
                true);

            this.AuthenticationPrompt?.Invoke(this, args);

            return new StaticPasswordCredential(username, args.Response);
        }

        //---------------------------------------------------------------------
        // ITextTerminal.
        //---------------------------------------------------------------------

        string ITextTerminal.TerminalType => SshShellChannel.DefaultTerminal;

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

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(exception))
            {
                if (this.ConnectionStatus == Status.Connected &&
                    errorType == TerminalErrorType.ConnectionLost)
                {
                    _ = OnConnectionLost(new ConnectionErrorEventArgs(exception))
                        .ContinueWith(_ => { });
                }
                else
                {
                    _ = OnConnectionFailed(new ConnectionErrorEventArgs(exception))
                        .ContinueWith(_ => { });
                }
            }
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        private async Task<SshShellChannel> ConnectAndTranslateErrorsAsync(
            TerminalSize initialSize)
        {
            try
            {
                var connection = new SshConnection(
                    this.Endpoint,
                    this.Credential,
                    (IKeyboardInteractiveHandler)this,
                    SynchronizationContext.Current)
                {
                    Banner = SshConnection.BannerPrefix + Install.UserAgent,
                    ConnectionTimeout = this.ConnectionTimeout,

                    //
                    // NB. Do not join worker thread as this could block the
                    // UI thread.
                    //
                    JoinWorkerThreadOnDispose = false
                };

                await connection.ConnectAsync()
                    .ConfigureAwait(false);

                return await connection
                    .OpenShellAsync(
                        (ITextTerminal)this,
                        initialSize)
                    .ConfigureAwait(false);
            }
            catch (Libssh2Exception e) when (
                e.ErrorCode == LIBSSH2_ERROR.PUBLICKEY_UNVERIFIED &&
                this.Credential is PlatformCredential platformCredential &&
                platformCredential.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
            {
                throw new OsLoginAuthenticationFailedException(
                    "Authenticating to the VM failed. Possible reasons for this " +
                    "error include:\n\n" +
                    " - The VM is configured to require 2-step verification,\n" +
                    "   and you haven't set up 2SV for your user account\n" +
                    " - The guest environment is misconfigured or not running",
                    e,
                    HelpTopics.TroubleshootingOsLogin);
            }
            catch (Libssh2Exception e) when (
                e.ErrorCode == LIBSSH2_ERROR.AUTHENTICATION_FAILED &&
                this.Credential is PlatformCredential platformCredential)
            {
                if (platformCredential.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
                {
                    var outdatedMessage = platformCredential.Signer is OsLoginCertificateSigner
                        ? " - The VM is running an outdated version of the guest environment \n" +
                          "   that doesn't support certificate-based authentication\n"
                        : string.Empty;

                    var message =
                        "Authenticating to the VM failed. Possible reasons for this " +
                        "error include:\n\n" +
                        " - You don't have sufficient access to log in\n" +
                        outdatedMessage +
                        " - The VM's guest environment is misconfigured or not running\n\n" +
                        "To log in, you need all of the following roles:\n\n" +
                        " 1. 'Compute OS Login' or 'Compute OS Admin Login'\n" +
                        " 2. 'Service Account User' (if the VM uses a service account)\n" +
                        " 3. 'Compute OS Login External User'\n" +
                        "    (if the VM belongs to a different GCP organization)\n\n" +
                        "Note that it might take several minutes for IAM policy changes to take effect.";

                    throw new OsLoginAuthenticationFailedException(
                        message,
                        e,
                        HelpTopics.GrantingOsLoginRoles);
                }
                else
                {
                    throw new MetadataKeyAuthenticationFailedException(
                        "Authentication failed. Verify that the Compute Engine guest environment " +
                        "is installed on the VM and that the agent is running.",
                        e,
                        HelpTopics.ManagingMetadataAuthorizedKeys);
                }
            }
            catch (Libssh2Exception e) when (
                e.ErrorCode == LIBSSH2_ERROR.KEY_EXCHANGE_FAILURE &&
                Environment.OSVersion.Version.Build <= 10000)
            {
                //
                // Libssh2's CNG support requires Windows 10+.
                //
                throw new PlatformNotSupportedException(
                    "SSH is not supported on this version of Windows");
            }
        }

        public override async Task ConnectAsync(TerminalSize initialSize)
        {
            Debug.Assert(this.View != null);
            Debug.Assert(this.ViewInvoker != null);

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
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
                    ApplicationTraceSource.Log.TraceError(e);

                    await OnConnectionFailed(new ConnectionErrorEventArgs(e))
                        .ConfigureAwait(true);

                    this.sshChannel = null;
                }
            }
        }

        public override async Task DisconnectAsync()
        {
            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
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
            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(newSize))
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

            using (ApplicationTraceSource.Log.TraceMethod().WithoutParameters())
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
                        return await this.jobService.RunAsync<bool>(
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
                                        Win32Filename.EscapeFilename(file.Name)));
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
                                    quarantineTasks.Add(this.quarantine.ScanAsync(
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

            using (ApplicationTraceSource.Log.TraceMethod().WithParameters(files))
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
                    return await this.jobService.RunAsync<bool>(
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
                this.Credential.Dispose();
            }
        }
    }

    public class AuthenticationPromptEventArgs
    {
        public bool IsPasswordPrompt { get; }
        public string Caption { get; }
        public string Prompt { get; }
        public string Response { get; set; }

        public AuthenticationPromptEventArgs(
            string caption,
            string prompt,
            bool isPasswordPrompt)
        {
            this.Caption = caption;
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
