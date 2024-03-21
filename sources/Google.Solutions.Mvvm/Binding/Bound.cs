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
using System;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Non-threadsafe set-once, read-many container.
    /// 
    /// Intended to be used in views for variables that are initialized
    /// during the Bind() call. These variables tend to be flagged as 
    /// "can be null" by the compiler because the compiler is unaware
    /// that a Bind() call is guaranteed to happen for views.
    /// 
    /// By wrapping member variables in a Bound<>, we can silence
    /// the compiler.
    /// </summary>
    public struct Bound<T> where T : class
    {
        private T value;

        public readonly bool HasValue
        {
            get => this.value != null;
        }

        public T Value
        {
            readonly get
            {
                if (this.value == null)
                {
                    throw new InvalidOperationException(
                        "The variable has not been bound yet");
                }

                return this.value.ExpectNotNull(nameof(this.value));
            }
            set
            {
                if (this.value != null)
                {
                    throw new InvalidOperationException(
                        "The variable has already been bound");
                }

                this.value = value;
            }
        }

        public static implicit operator T(Bound<T> value)
        {
            return value.Value;
        }

        public readonly override string ToString()
        {
            if (!this.HasValue)
            {
                return "";
            }

            return this.value.ToString();
        }
    }
}
