//
// Copyright 2023 Google LLC
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
using System.Text.RegularExpressions;

namespace Google.Solutions.Apis.Auth.Iam
{
    /// <summary>
    /// Locator for a Workforce pool provider.
    /// </summary>
    public class WorkforcePoolProviderLocator
        : IEquatable<WorkforcePoolProviderLocator>, IPoolProviderLocator
    {
        /// <summary>
        /// Location, typically 'global'.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// ID of the workforce pool.
        /// </summary>
        public string Pool { get; }

        /// <summary>
        /// ID of the workforce pool provider.
        /// </summary>
        public string Provider { get; }

        public WorkforcePoolProviderLocator(string location, string pool, string provider)
        {
            this.Location = location.ExpectNotEmpty(nameof(location));
            this.Pool = pool.ExpectNotEmpty(nameof(pool));
            this.Provider = provider.ExpectNotEmpty(nameof(provider));
        }

        public static WorkforcePoolProviderLocator? Parse(string? resourceReference)
        {
            if (TryParse(resourceReference, out var locator))
            {
                return locator;
            }
            else
            {
                throw new ArgumentException(
                    $"'{resourceReference}' is not a valid workforce pool provider locator");
            }
        }

        public static bool TryParse(
            string? resourceReference,
            out WorkforcePoolProviderLocator? locator)
        {
            if (string.IsNullOrEmpty(resourceReference))
            {
                locator = null;
                return false;
            }

            var match = new Regex("^locations/(.*)/workforcePools/(.*)/providers/(.*)$")
                .Match(resourceReference);
            if (match.Success &&
                !string.IsNullOrEmpty(match.Groups[1].Value) &&
                !string.IsNullOrEmpty(match.Groups[2].Value) &&
                !string.IsNullOrEmpty(match.Groups[3].Value))
            {
                locator = new WorkforcePoolProviderLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public override string ToString()
        {
            return $"locations/{this.Location}/workforcePools/{this.Pool}/providers/{this.Provider}";
        }

        public override int GetHashCode()
        {
            return
                this.Pool.GetHashCode() ^
                this.Provider.GetHashCode();
        }

        public bool Equals(WorkforcePoolProviderLocator other)
        {
            return other is object &&
                this.Location == other.Location &&
                this.Pool == other.Pool &&
                this.Provider == other.Provider;
        }

        public override bool Equals(object obj)
        {
            return obj is WorkforcePoolProviderLocator locator && Equals(locator);
        }

        public static bool operator ==(WorkforcePoolProviderLocator obj1, WorkforcePoolProviderLocator obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(WorkforcePoolProviderLocator obj1, WorkforcePoolProviderLocator obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
