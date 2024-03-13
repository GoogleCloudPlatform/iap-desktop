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

using Google.Solutions.Common.Util;
using System;
using System.Linq;

namespace Google.Solutions.Settings.Collection
{
    /// <summary>
    /// Base class for all settings repositories.
    /// </summary>
    public abstract class RepositoryBase<TCollection>
        : IRepository<TCollection>
        where TCollection : ISettingsCollection
    {
        protected ISettingsStore Store { get; }

        protected RepositoryBase(ISettingsStore store)
        {
            this.Store = store.ExpectNotNull(nameof(store));
        }

        public virtual TCollection GetSettings()
        {
            return LoadSettings(this.Store);
        }

        public virtual void SetSettings(TCollection collection)
        {
            foreach (var setting in collection.Settings.Where(s => s.IsDirty))
            {
                this.Store.Write(setting);
            }
        }

        public void ClearSettings()
        {
            this.Store.Clear();
        }

        protected abstract TCollection LoadSettings(ISettingsStore store);

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Store.Dispose();
        }
    }
}
