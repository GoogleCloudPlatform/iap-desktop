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

using Google.Solutions.Apis;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Windows
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
        /// <summary>
        /// Run a potentially slow or blocking function in the background while
        /// shwowing a wait animation in the GUI. The GUI is being kept responsive
        /// while the background job is running.
        /// 
        /// If a reauth error is thrown, a reauthorization is performed and the
        /// job is retried. For this logic to work, it is important that the
        /// jobFunc does not swallow reauth errors.
        /// 
        /// If necessary, add a handler like:
        /// 
        /// catch (Exception e) when (e.IsReauthError())
        /// {
        ///     // Propagate reauth errors so that the reauth logic kicks in.
        ///     throw;
        /// }
        ///
        /// to ensure reauth errors are propagated.
        /// </summary>
        Task<T> RunAsync<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc);

        Task RunAsync(
            JobDescription jobDescription,
            Func<CancellationToken, Task> jobFunc);
    }

    public class JobService : IJobService
    {
        private readonly IJobHost host;
        private readonly IAuthorization authorization;

        public JobService(IAuthorization authorization, IJobHost host)
        {
            this.authorization = authorization;
            this.host = host;
        }

        private Task<T> RunWithUserFeedbackAsync<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc)
        {
            var completionSource = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource();

            Exception? exception = null;

            var feedback = this.host.ShowFeedback(jobDescription, cts);

            _ = Task.Run(async () =>
            {
                using (cts)
                {
                    try
                    {
                        // Now that we are on thread pool thread, run the job func and
                        // allow the continuation to happen on any thread.
                        var result = await jobFunc(cts.Token).ConfigureAwait(continueOnCapturedContext: false);

                        while (!feedback.IsShowing)
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
                                feedback.Finish();
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
                                feedback.Finish();
                            }
                        }), null);

                        // Unblock the awaiter.
                        completionSource.SetException(e);
                    }
                }
            });

            // This call blocks if it's foreground feedback.
            feedback.Start();

            if (exception != null)
            {
                throw exception.Unwrap();
            }

            return completionSource.Task;
        }

        public async Task<T> RunAsync<T>(
            JobDescription jobDescription,
            Func<CancellationToken, Task<T>> jobFunc)
        {
            Debug.Assert(!this.host.Invoker.InvokeRequired, "RunInBackground must be called on UI thread");

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    return await
                        RunWithUserFeedbackAsync(
                            jobDescription,
                            jobFunc)
                        .ConfigureAwait(true);  // Continue on UI thread.
                }
                catch (Exception e) when (e.IsReauthError())
                {
                    //
                    // Reauth required or authorization has been revoked.
                    //
                    if (attempt >= 1)
                    {
                        //
                        // Retrying a second time is pointless.
                        //
                        throw;
                    }
                    else
                    {
                        //
                        // Request user to reauthorize, then we'll try again.
                        //
                        Debug.Assert(!this.host.Invoker.InvokeRequired);
                        this.host.Reauthorize();
                    }
                }
            }
        }

        public Task RunAsync(
            JobDescription jobDescription,
            Func<CancellationToken, Task> jobFunc)
        {
            return RunAsync<string>(
                jobDescription,
                async cancellationToken =>
                {
                    await jobFunc(cancellationToken).ConfigureAwait(true);
                    return string.Empty;
                });
        }
    }

    public class JobDescription
    {
        public string StatusMessage { get; }

        public JobUserFeedbackType Feedback { get; }

        public JobDescription(string statusMessage)
            : this(statusMessage, JobUserFeedbackType.ForegroundFeedback)
        {
        }

        public JobDescription(string statusMessage, JobUserFeedbackType feedbackType)
        {
            this.StatusMessage = statusMessage;
            this.Feedback = feedbackType;
        }
    }

    public enum JobUserFeedbackType
    {
        /// <summary>
        /// Show job feedback in background, permitting other UI interaction.
        /// </summary>
        BackgroundFeedback,

        /// <summary>
        /// Show job feedback in foreground, blocking all other UI interaction 
        /// (without blocking the UI thread).
        /// </summary>
        ForegroundFeedback,
    }

    /// <summary>
    /// Interface to implement by a window that controls jobs.
    /// </summary>
    public interface IJobHost
    {
        /// <summary>
        /// Invoker used by window thread.
        /// </summary>
        ISynchronizeInvoke Invoker { get; }

        /// <summary>
        /// Reauthorize, called when a job failed because the
        /// session expired.
        /// </summary>
        void Reauthorize();

        /// <summary>
        /// Display feedback about a job.
        /// </summary>
        IJobUserFeedback ShowFeedback(
            JobDescription jobDescription,
            CancellationTokenSource cts);
    }

    public interface IJobUserFeedback
    {
        void Start();
        bool IsShowing { get; }
        void Finish();
    }
}
