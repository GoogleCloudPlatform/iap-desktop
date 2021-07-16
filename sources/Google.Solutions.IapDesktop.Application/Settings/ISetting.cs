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

using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Settings
{
    public interface ISetting
    {
        string Key { get; }
        string Title { get; }
        string Description { get; }
        string Category { get; }
        object Value { get; set; }
        bool IsDefault { get; }
        bool IsDirty { get; }
        bool IsReadOnly { get; }
        ISetting OverlayBy(ISetting setting);
        void Reset();

        Type ValueType { get; }
    }

    public interface ISetting<T> : ISetting
    {
        T DefaultValue { get; }
        ISetting<T> OverlayBy(ISetting<T> setting);
    }

    public interface ISettingsCollection
    {
        IEnumerable<ISetting> Settings { get; }
    }

    public interface IPersistentSettingsCollection : ISettingsCollection
    {
        void Save();
    }

    public interface IPersistentSettingsCollection<out TCollection> : IPersistentSettingsCollection
        where TCollection : ISettingsCollection
    {
        TCollection TypedCollection { get; }
    }
}
