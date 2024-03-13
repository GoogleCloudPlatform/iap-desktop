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

namespace Google.Solutions.Settings
{
    /// <summary>
    /// Store for settings.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>
        /// Read a value and map it to a settings object.
        /// </summary>
        ISetting<T> Read<T>(
            string name,
            string displayName,
            string description,
            string category,
            T defaultValue,
            ValidateDelegate<T> validate = null);

        /// <summary>
        /// Write value back to the store.
        /// </summary>
        void Write(ISetting setting);
    }

    /// <summary>
    /// Delegate for validating if a given value falls
    /// within the permitted range of a setting.
    /// </summary>
    public delegate bool ValidateDelegate<T>(T value);
}