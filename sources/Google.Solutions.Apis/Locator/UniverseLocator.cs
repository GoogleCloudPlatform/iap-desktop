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

namespace Google.Solutions.Apis.Locator
{
    /// <summary>
    /// Locator for a universe. 
    /// </summary>
    public class UniverseLocator : ILocator, IEquatable<UniverseLocator>
    {
        /// <summary>
        /// Cloud, the only universe currently supported.
        /// </summary>
        public static readonly UniverseLocator Cloud = new UniverseLocator("gdu");

        internal UniverseLocator(string id)
        {
            this.Id = id;
        }

        public string Id { get; }

        public string ResourceType
        {
            get => "universes";
        }

        public bool Equals(UniverseLocator? other)
        {
            return other is object && this.Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is UniverseLocator locator && Equals(locator);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.ResourceType}/{this.Id}";
        }

        public static bool operator ==(UniverseLocator? obj1, UniverseLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(UniverseLocator? obj1, UniverseLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
