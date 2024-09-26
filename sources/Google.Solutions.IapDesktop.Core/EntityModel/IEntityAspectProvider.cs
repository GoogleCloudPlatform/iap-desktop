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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Base interface for aspect providers.
    /// </summary>
    /// <remarks>
    /// Implementing types must also implement
    /// the generic version of this interface.
    /// </remarks>
    public interface IEntityAspectProvider
    {
    }

    /// <summary>
    /// Provides ancillary data (an "aspect") about an entity.
    /// </summary>
    public interface IAsyncEntityAspectProvider<TLocator, TAspect> : IEntityAspectProvider
        where TLocator : ILocator
        where TAspect : class
    {
        Task<TAspect?> QueryAspectAsync(
            TLocator locator,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Provides ancillary data (an "aspect") about an entity.
    /// </summary>
    public interface IEntityAspectProvider<TLocator, TAspect> : IEntityAspectProvider
        where TLocator : ILocator
        where TAspect : class
    {
        TAspect? QueryAspect(TLocator locator);
    }
}
