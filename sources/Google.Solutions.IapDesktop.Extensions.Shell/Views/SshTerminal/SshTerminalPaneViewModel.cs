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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Auth;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{

    public class SshTerminalPaneViewModel : TerminalPaneViewModelBase, ISshAuthenticator, ITextTerminal
    {
        private readonly CultureInfo language;
        private readonly IPEndPoint endpoint;
        private readonly AuthorizedKeyPair authorizedKey;
        private readonly TimeSpan connectionTimeout;
        private RemoteShellChannel sshChannel = null;
        private IConfirmationDialog confirmationDialog;
        private readonly IJobService jobService;

        public event EventHandler<AuthenticationPromptEventArgs> AuthenticationPrompt;

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        public SshTerminalPaneViewModel(
            IEventService eventService,
            IJobService jobService,
            IConfirmationDialog confirmationDialog,
            InstanceLocator vmInstance,
            IPEndPoint endpoint,
            AuthorizedKeyPair authorizedKey,
            CultureInfo language,
            TimeSpan connectionTimeout)
            : base(eventService, vmInstance)
        {
            this.jobService = jobService;
            this.confirmationDialog = confirmationDialog;
            this.endpoint = endpoint;
            this.authorizedKey = authorizedKey;
            this.language = language;
            this.connectionTimeout = connectionTimeout;
        }

        //---------------------------------------------------------------------
        // ISshAuthenticator.
        //---------------------------------------------------------------------

        string ISshAuthenticator.Username => this.authorizedKey.Username;

        ISshKeyPair ISshAuthenticator.KeyPair => this.authorizedKey.KeyPair;

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

        CultureInfo ITextTerminal.Locale => this.language;

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
                        this.endpoint,
                        (ISshAuthenticator)this,
                        SynchronizationContext.Current)
                {
                    Banner = SshSession.BannerPrefix + Globals.UserAgent,
                    ConnectionTimeout = this.connectionTimeout,

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
                e.ErrorCode == LIBSSH2_ERROR.AUTHENTICATION_FAILED &&
                this.authorizedKey.AuthorizationMethod == KeyAuthorizationMethods.Oslogin)
            {
                throw new OsLoginAuthenticationFailedException(
                    "You do not have sufficient permissions to access this VM instance.\n\n" +
                    "To perform this action, you need the following roles (or an equivalent custom role):\n\n" +
                    " 1. 'Compute OS Login' or 'Compute OS Admin Login'\n" +
                    " 2. 'Service Account User' (if the VM uses a service account)\n" +
                    " 3. 'Compute OS Login External User' (if the VM belongs to a different GCP organization\n",
                    e,
                    HelpTopics.GrantingOsLoginRoles);
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

        public async Task<bool> UploadFilesAsync(IEnumerable<FileInfo> files)
        {
            if (this.sshChannel == null)
            {
                return false;
            }

            using (ApplicationTraceSources.Default.TraceMethod().WithParameters(files))
            {
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
                        $"file(s) to {this.Instance.Name}?\n\n - " +
                        string.Join("\n - ", fileNamesToUpload);
                    if (conflicts.Any())
                    {
                        message +=
                            "\n\nThe following files already exist on the server and will be replaced:\n\n - " +
                            string.Join("\n - ", conflicts);
                    }

                    if (this.confirmationDialog.Confirm(this.View, message, "Upload") != DialogResult.Yes)
                    {
                        return false;
                    }

                    //
                    // Upload files in a background job, allowing
                    // cancellation.
                    //
                    // TODO: use injection
                    using (var progressDialog = new OperationProgressDialog().ShowCopyDialog(
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
                this.authorizedKey.Dispose();
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
}
