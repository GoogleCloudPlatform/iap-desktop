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
    /// Identifies a type of entity.
    /// </summary>
    public class EntityTypeLocator : ILocator, IEquatable<EntityTypeLocator>
    {
        public Type Type { get; }
        public string ResourceType => "types";

        internal EntityTypeLocator(Type type)
        {
            this.Type = type;
        }

        public override string ToString()
        {
            return $"{this.ResourceType}/{this.Type.FullName}";
        }

        public override bool Equals(object obj)
        {
            return obj is EntityTypeLocator locator && Equals(locator);
        }

        public override int GetHashCode()
        {
            return this.Type.GetHashCode();
        }

        public bool Equals(EntityTypeLocator? other)
        {
            return other is object && other.Type == this.Type;
        }

        public static bool operator ==(EntityTypeLocator? obj1, EntityTypeLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(EntityTypeLocator? obj1, EntityTypeLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }

    /// <summary>
    /// A type of entity.
    /// </summary>
    public class EntityType : IEntity<EntityTypeLocator>
    {
        public EntityType(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; }

        public EntityTypeLocator Locator => new EntityTypeLocator(this.Type);

        public string DisplayName => this.Type.Name;
    }
}
