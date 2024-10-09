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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Google.Solutions.Common.Linq
{
    /// <summary>
    /// Utility methods for creating lists.
    /// </summary>
    public static class Lists
    {
        /// <summary>
        /// Create an collection for a nullable value.
        /// </summary>
        /// <returns>
        /// Empty collection if the object is null, a 
        /// single-element enumerable otherwise.
        /// </returns>
        public static IList<T> FromNullable<T>(T? nullable) where T : class
        {
            return nullable == null
                ? Array.Empty<T>()
                : new T[] { nullable };
        }
    }
}
