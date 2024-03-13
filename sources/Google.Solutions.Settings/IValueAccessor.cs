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
    /// Accessor that automatically performs necessary 
    /// type conversions.
    /// </summary>
    internal interface IValueAccessor<TSource, TValue>
    {
        /// <summary>
        /// Read stored value.
        /// </summary>
        /// <param name="value">result</param>
        /// <returns>true if found, false if value not set</returns>
        bool TryRead(TSource key, out TValue value);

        /// <summary>
        /// Store value.
        /// </summary>
        /// <param name="value"></param>
        void Write(TSource key, TValue value);

        /// <summary>
        /// Delete stored value.
        /// </summary>
        void Delete(TSource key);

        /// <summary>
        /// Check if a value is acceptable to be written.
        /// </summary>
        /// <returns></returns>
        bool IsValid(TValue value);
    }
}
