//
// Copyright 2024 Google LLC
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
using System.Collections.Generic;

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Exposes dictionary entries as settings.
    /// </summary>
    public class DictionarySettingsStore
        : SettingsStoreBase<IDictionary<string, string>>
    {
        private protected override IDictionary<string, string> ValueSource { get; }

        public DictionarySettingsStore(IDictionary<string, string> source)
        {
            this.ValueSource = source.ExpectNotNull(nameof(source));
        }

        /// <summary>
        /// Create an empty store.
        /// </summary>
        public static DictionarySettingsStore Empty()
        {
            return new DictionarySettingsStore(new Dictionary<string, string>());
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        private protected override IValueAccessor<IDictionary<string, string>, T> 
            CreateValueAccessor<T>(string valueName)
        {
            return DictionaryValueAccessor.Create<T>(valueName);
        }

        public override void Clear()
        {
            this.ValueSource.Clear();
        }
    }
}
