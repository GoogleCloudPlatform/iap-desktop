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
using System.Text.RegularExpressions;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.Apis.Locator
{
    /// <summary>
    /// Locator for organizations.
    /// </summary>
    public class OrganizationLocator : IEquatable<OrganizationLocator>
    {
        public OrganizationLocator(long organizationId)
        {
            this.Id = organizationId;
        }

        public long Id { get; }

        public string ResourceType
        {
            get => "organizations";
        }

        public static OrganizationLocator Parse(string s)
        {
            var match = new Regex("^organizations/(\\d*)$").Match(s);
            if (match.Success && 
                long.TryParse(match.Groups[1].Value, out var organizationId))
            {
                return new OrganizationLocator(organizationId);
            }
            else
            {
                throw new ArgumentException($"'{s}' is not a valid organization locator");
            }
        }

        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        public override string ToString()
        {
            return $"organizations/{this.Id}";
        }

        public bool Equals(OrganizationLocator? other)
        {
            return other is object && this.Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is OrganizationLocator locator && Equals(locator);
        }

        public static bool operator ==(OrganizationLocator? obj1, OrganizationLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(OrganizationLocator? obj1, OrganizationLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
