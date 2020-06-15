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
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    /// <summary>
    /// View model that maintains an LRU cache of models.
    /// </summary>
    public abstract class ModelCachingViewModelBase<TModelKey, TModel> : ViewModelBase
    {
        private readonly LeastRecentlyUsedCache<TModelKey, TModel> modelCache;

        private readonly TaskScheduler taskScheduler;
        private CancellationTokenSource tokenSourceForCurrentTask = null;

        public ModelCachingViewModelBase(int cacheCapacity)
        {
            // Capture the GUI thread scheduler.
            this.taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            this.modelCache = new LeastRecentlyUsedCache<TModelKey, TModel>(
                cacheCapacity);
        }

        public void BeginSwitchToModel(TModelKey key)
        {
            var model = this.modelCache.Lookup(key);
            if (model != null)
            {
                // Apply model synchronously.
                this.modelCache.Add(key, model);
                ApplyModel(model, true);
            }
            else
            {
                if (this.tokenSourceForCurrentTask != null)
                {
                    // Another asynchnous load/bind operation is ongoing.
                    // Cancel that one because we won't need its result.

                    TraceSources.IapDesktop.TraceVerbose("Cancelling previous model load task");
                    this.tokenSourceForCurrentTask.Cancel();
                    this.tokenSourceForCurrentTask = null;
                }

                // Load model.
                this.tokenSourceForCurrentTask = new CancellationTokenSource();
                LoadModelAsync(key, this.tokenSourceForCurrentTask.Token)
                    .ContinueWith(t =>
                    {
                        try
                        {
                            if (t.Result != null)
                            {
                                this.modelCache.Add(key, t.Result);
                                ApplyModel(t.Result, false);
                            }
                        }
                        catch (Exception e) when (e.IsCancellation())
                        {
                            TraceSources.IapDesktop.TraceVerbose("Model load cancelled");
                        }
                    },
                    this.tokenSourceForCurrentTask.Token,
                    TaskContinuationOptions.None,

                    // Continue on UI thread. 
                    // Note that there's a bug in the CLR that can cause
                    // TaskScheduler.FromCurrentSynchronizationContext() to become null.
                    // Therefore, use a task scheduler object captured previously.
                    // Cf. https://stackoverflow.com/questions/4659257/
                    this.taskScheduler);
            }
        }

        protected abstract Task<TModel> LoadModelAsync(
            TModelKey key,
            CancellationToken token);

        protected abstract void ApplyModel(
            TModel model,
            bool cached);
    }
}
