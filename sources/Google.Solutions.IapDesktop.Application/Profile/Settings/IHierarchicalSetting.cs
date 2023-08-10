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

namespace Google.Solutions.IapDesktop.Application.Profile.Settings
{
    /// <summary>
    /// Base interface for a setting.
    /// </summary>
    public interface ISetting
    {
        string Key { get; }
        string Title { get; }
        string Description { get; }
        string Category { get; }
        object Value { get; set; }
        bool IsDefault { get; }
        bool IsDirty { get; }
        bool IsSpecified { get; }
        bool IsReadOnly { get; }
        ISetting OverlayBy(ISetting setting);
        void Reset();
        Type ValueType { get; }
    }

    /// <summary>
    /// String-valued setting.
    /// </summary>
    public interface IStringSetting : ISetting
    {
        string StringValue { get; set; }
    }

    /// <summary>
    /// SecureString-valued setting.
    /// </summary>
    public interface ISecureStringSetting : ISetting
    {
        string ClearTextValue { get; set; }
    }

    /// <summary>
    /// Bool-valued setting.
    /// </summary>
    public interface IBoolSetting : ISetting
    {
        bool BoolValue { get; set; }
    }

    /// <summary>
    /// Int-valued setting.
    /// </summary>
    public interface IIntSetting : ISetting
    {
        int IntValue { get; set; }
    }

    /// <summary>
    /// Long-valued setting.
    /// </summary>
    public interface ILongSetting : ISetting
    {
        long LongValue { get; set; }
    }

    /// <summary>
    /// Enum-valued setting.
    /// </summary>
    public interface IEnumSetting<TEnum> : ISetting
        where TEnum : struct
    {
        TEnum EnumValue { get; set; }
    }

    public interface IHierarchicalSetting<T> : ISetting
    {
        T DefaultValue { get; }
        IHierarchicalSetting<T> OverlayBy(IHierarchicalSetting<T> setting);
    }
}
