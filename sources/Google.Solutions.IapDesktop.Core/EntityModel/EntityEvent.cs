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
using System;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Base class for entity-related events.
    /// </summary>
    public abstract class EntityEvent
    {
        /// <summary>
        /// Locator of the affected entity.
        /// </summary>
        public ILocator Locator { get; }

        protected EntityEvent(ILocator locator)
        {
            this.Locator = locator;
        }
    }

    /// <summary>
    /// Indicates that one or more properties were changed.
    /// </summary>
    public class EntityPropertyChangedEvent : EntityEvent
    {
        /// <summary>
        /// Aspect of which a property changed.
        /// </summary>
        public Type Aspect { get; }

        /// <summary>
        /// Name of the property that changed.
        /// </summary>
        /// <remarks>
        /// null if more than a single property changed.
        /// </remarks>
        public string? PropertyName { get; }

        public EntityPropertyChangedEvent(
            ILocator locator,
            Type aspect, 
            string? propertyName) : base(locator)
        {
            this.Aspect = aspect;
            this.PropertyName = propertyName;
        }
    }

    /// <summary>
    /// Indicates that an entity was removed.
    /// </summary>
    public class EntityRemovedEvent : EntityEvent
    {
        public EntityRemovedEvent(ILocator locator) : base(locator)
        {
        }
    }
}
