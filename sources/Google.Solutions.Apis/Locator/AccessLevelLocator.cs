//
// Copyright 2020 Google LLC
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

namespace Google.Solutions.Apis.Locator
{
    /// <summary>
    /// Locator for access levels.
    /// 
    /// NB. Although access levels are global, their locator does not 
    /// follow the same conventions as other (Compute) resources.
    /// </summary>
    public class AccessLevelLocator : ILocator, IEquatable<AccessLevelLocator>
    {
        public string AccessPolicy { get; }
        public string AccessLevel { get; }

        public string ResourceType
        {
            get => "accessLevels";
        }

        public AccessLevelLocator(string accessPolicy, string accessLevel)
        {
            this.AccessPolicy = accessPolicy;
            this.AccessLevel = accessLevel;
        }

        public static bool TryParse(string path, out AccessLevelLocator? locator)
        {
            var match = new Regex("accessPolicies/(.*)/accessLevels/(.*)")
                .Match(path);
            if (match.Success)
            {
                locator = new AccessLevelLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static AccessLevelLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid access level locator");
            }
        }

        public override int GetHashCode()
        {
            return
                this.AccessPolicy.GetHashCode() ^
                this.AccessLevel.GetHashCode();
        }

        public override string ToString()
        {
            return $"accessPolicies/{this.AccessPolicy}/accessLevels/{this.AccessLevel}";
        }

        public bool Equals(AccessLevelLocator? other)
        {
            return other is object &&
                this.AccessPolicy == other.AccessPolicy &&
                this.AccessLevel == other.AccessLevel;
        }

        public override bool Equals(object? obj)
        {
            return obj is AccessLevelLocator locator && Equals(locator);
        }

        public static bool operator ==(AccessLevelLocator? obj1, AccessLevelLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(AccessLevelLocator? obj1, AccessLevelLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }
}
