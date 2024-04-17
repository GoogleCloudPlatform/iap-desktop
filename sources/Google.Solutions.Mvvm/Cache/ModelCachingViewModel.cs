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

using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Binding;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Cache
{
    /// <summary>
    /// View model that maintains an LRU cache of models.
    /// </summary>
    public abstract class ModelCachingViewModelBase<TModelKey, TModel> : ViewModelBase
        where TModel : class
        where TModelKey : class
    {
        private readonly LeastRecentlyUsedCache<TModelKey, TModel> modelCache;

        private CancellationTokenSource? tokenSourceForCurrentTask = null;

        /// <summary>
        /// Current model.
        /// </summary>
        protected TModel? Model { get; private set; }

        /// <summary>
        /// Current key.
        /// </summary>
        protected TModelKey? ModelKey { get; private set; }

        protected ModelCachingViewModelBase(int cacheCapacity)
        {
            this.modelCache = new LeastRecentlyUsedCache<TModelKey, TModel>(
                cacheCapacity);
        }

        public async Task SwitchToModelAsync(TModelKey key)
        {
            if (this.Model != null)
            {
                //
                // Reset.
                //

                this.Model = default(TModel);
                ApplyModel(false);
            }

            this.ModelKey = key;
            var model = this.modelCache.Lookup(key);
            if (model != null)
            {
                //
                // Apply model synchronously.
                //

                this.modelCache.Add(key, model);
                this.Model = model;
                ApplyModel(true);
            }
            else
            {
                if (this.tokenSourceForCurrentTask != null)
                {
                    //
                    // Another asynchronous load/bind operation is ongoing.
                    // Cancel that one because we won't need its result.
                    //

                    CommonTraceSource.Log.TraceVerbose("Cancelling previous model load task");
                    this.tokenSourceForCurrentTask.Cancel();
                    this.tokenSourceForCurrentTask = null;
                }

                //
                // Load model.
                //

                this.tokenSourceForCurrentTask = new CancellationTokenSource();
                try
                {
                    this.Model = await LoadModelAsync(key, this.tokenSourceForCurrentTask.Token)
                        .ConfigureAwait(true);  // Back to original (UI) thread.

                    if (this.Model != null)
                    {
                        this.modelCache.Add(key, this.Model);
                    }

                    ApplyModel(false);
                }
                catch (Exception e) when (e.IsCancellation())
                {
                    CommonTraceSource.Log.TraceVerbose("Model load cancelled");
                }
            }
        }

        protected Task InvalidateAsync()
        {
            if (this.ModelKey == null)
            {
                return Task.CompletedTask;
            }

            this.modelCache.Remove(this.ModelKey);
            return SwitchToModelAsync(this.ModelKey);
        }

        protected abstract Task<TModel?> LoadModelAsync(
            TModelKey key,
            CancellationToken token);

        protected abstract void ApplyModel(bool cached);
    }
}
