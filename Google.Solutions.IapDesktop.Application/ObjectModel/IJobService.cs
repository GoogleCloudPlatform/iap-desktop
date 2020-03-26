//
// Copyright 2010 Google LLC
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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.IapDesktop.Application.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    /// <summary>
    /// Allows long-running background jobs to be run. While the job is run,
    /// a wait dialog is shown to visually block the UI without blocking the
    /// UI thread.
    /// 
    /// Invoker thread: UI thread (prerequisite)
    /// Execution Thread: Thread pool (ensured by JobService)
    /// Continuation thread: UI thread (ensured by JobService)
    /// </summary>
    public interface IJobService
    {
        Task<T> RunInBackground<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc);
    }

    public class JobService : IJobService
    {
        private readonly IJobHost host;
        private readonly IAuthorizationService authService;

        public JobService(IServiceProvider serviceProvider)
        {
            this.host = serviceProvider.GetService<IJobHost>();
            this.authService = serviceProvider.GetService<IAuthorizationService>();
        }

        public Task<T> RunInBackgroundWithoutReauth<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc)
        {
            Debug.Assert(!this.host.Invoker.InvokeRequired, "RunInBackground must be called on UI thread");

            var completionSource = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource();

            Exception exception = null;

            Task.Run(async () =>
            {
                using (cts)
                {
                    try
                    {
                        // Now that we are on thread pool thread, run the job func and
                        // allow the continuation to happen on any thread.
                        var result = await jobFunc(cts.Token).ConfigureAwait(continueOnCapturedContext: false);

                        while (!host.IsWaitDialogShowing)
                        {
                            // If the function finished fast, it is possible that the dialog
                            // has not even been shown yet - that would cause BeginInvoke
                            // to fail.
                            Thread.Sleep(10);
                        }

                        if (cts.IsCancellationRequested)
                        {
                            // The operation was cancelled (probably by the user hitting a 
                            // Cancel button, but the job func ran to completion. 
                            completionSource.SetException(new TaskCanceledException());
                        }
                        else
                        {
                            // Close the dialog immediately...
                            this.host.Invoker.Invoke((Action)(() =>
                            {
                                host.CloseWaitDialog();
                            }), null);

                            // ...then run the GUI function. If this function opens
                            // another dialog, the two dialogs will not overlap.
                            this.host.Invoker.BeginInvoke((Action)(() =>
                            {
                                // Unblock the awaiter.
                                completionSource.SetResult(result);
                            }), null);
                        }
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        this.host.Invoker.BeginInvoke((Action)(() =>
                        {
                            // The 'Cancel' button closes the dialog, do not close again
                            // if the user clicked that button.
                            if (!cts.IsCancellationRequested)
                            {
                                host.CloseWaitDialog();
                            }
                        }), null);

                        // Unblock the awaiter.
                        completionSource.SetException(e);
                    }
                }
            });


            this.host.ShowWaitDialog(jobDescription, cts);

            if (exception != null)
            {
                throw exception.Unwrap();
            }

            return completionSource.Task;
        }

        public async Task<T> RunInBackground<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return await RunInBackgroundWithoutReauth(jobDescription, jobFunc);
                }
                catch (TokenResponseException tokenException)
                    when (tokenException.Error.Error == "invalid_grant")
                {
                    // Reauth required or authorization has been revoked.
                    if (attempt >= 1)
                    {
                        // Retrying a second time is pointless.
                        throw;
                    }
                    else if (this.host.ConfirmReauthorization())
                    {
                        // Reauthorize. This might take a while since the user has to use 
                        // a browser - show the WaitDialog in the meantime.
                        await RunInBackgroundWithoutReauth(
                            new JobDescription("Authorizing"),
                            async _ =>
                            {
                                await this.authService.Authorization.ReauthorizeAsync();
                                return default(T);
                            });
                    }
                    else
                    {
                        throw new TaskCanceledException("Reauthorization aborted");
                    }
                }
            }
        }
    }

    public class JobDescription
    {
        public string StatusMessage { get; }

        public JobDescription(string statusMessage)
        {
            this.StatusMessage = statusMessage;
        }
    }

    public interface IJobHost
    {
        ISynchronizeInvoke Invoker { get; }
        void ShowWaitDialog(JobDescription jobDescription, CancellationTokenSource cts);
        bool IsWaitDialogShowing { get; }
        void CloseWaitDialog();
        bool ConfirmReauthorization();
    }
}
