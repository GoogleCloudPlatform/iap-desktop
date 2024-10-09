﻿//
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

using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.Common.Linq
{
    /// <summary>
    /// Utility methods for creating Enumerables.
    /// </summary>
    public static class Enumerables
    {
        /// <summary>
        /// Create an enumerable for a nullable value.
        /// </summary>
        /// <returns>
        /// Empty enumerable if the object is null, a 
        /// single-element enumerable otherwise.
        /// </returns>
        public static IEnumerable<T> FromNullable<T>(T? nullable) where T : class
        {
            return nullable == null
                ? Enumerable.Empty<T>()
                : new T[] { nullable };
        }
    }
}
