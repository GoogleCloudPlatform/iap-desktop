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

using Google.Solutions.Apis.Locator;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// Base interface for entity objects.
    /// </summary>
    /// <remarks>
    /// Implement IEntity<T> instead of this interface.,
    /// </remarks>
    public interface IEntity
    {
        /// <summary>
        /// Display name, might differ from the name
        /// used in the locator.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Locator for this item.
        /// </summary>
        ILocator Locator { get; }
    }

    /// <summary>
    /// Represents an entity.
    /// </summary>
    public interface IEntity<TLocator> : IEntity where TLocator : ILocator
    {
    }
}
