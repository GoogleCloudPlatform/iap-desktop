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

using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public static class PersistentSettingsCollection
    {
        private class PersistentCollection<TCollection> : IPersistentSettingsCollection<TCollection>
            where TCollection : ISettingsCollection
        {
            private readonly Action<TCollection> saveFunc;
            public TCollection TypedCollection { get; }

            public PersistentCollection(
                TCollection settingsCollection,
                Action<TCollection> saveFunc)
            {
                this.TypedCollection = settingsCollection;
                this.saveFunc = saveFunc;
            }

            public IEnumerable<ISetting> Settings => this.TypedCollection.Settings;

            public void Save() => this.saveFunc(this.TypedCollection);
        }

        public static IPersistentSettingsCollection<TCollection> ToPersistentSettingsCollection<TCollection>(
            this TCollection collection,
            Action<TCollection> saveFunc)
            where TCollection : ISettingsCollection
        {
            return new PersistentCollection<TCollection>(collection, saveFunc);
        }
    }
}
