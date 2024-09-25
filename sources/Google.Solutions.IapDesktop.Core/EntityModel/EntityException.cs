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
    /// Indicates that a requested entity was not found.
    /// </summary>
    public abstract class EntityException : Exception
    {
        public ILocator Locator { get; }

        protected EntityException(
            string message,
            ILocator locator) 
            : base(message)
        {
            this.Locator = locator;
        }
    }

    /// <summary>
    /// Indicates that a requested entity was not found.
    /// </summary>
    public class EntityNotFoundException : EntityException
    {
        public EntityNotFoundException(ILocator locator) 
            : base($"The entity '{locator}' is not available", locator)
        {
        }
    }

    /// <summary>
    /// Indicates that a requested aspect was not found.
    /// </summary>
    public class EntityAspectNotFoundException : EntityException
    {
        public EntityAspectNotFoundException(
            ILocator locator,
            Type aspectType)
            : base(
                  $"The information '{aspectType.Name}' is not available for entity '{locator}'",
                  locator)
        {
        }
    }
}
