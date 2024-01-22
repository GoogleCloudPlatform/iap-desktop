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

using Google.Solutions.Apis.Locator;
using System.Text.RegularExpressions;

namespace Google.Solutions.Apis.Compute
{
    /// <summary>
    /// Compute Engine-internal DNS name.
    /// </summary>
    public abstract class InternalDnsName
    {
        //
        // "Official" patterns from discovery document
        // https://www.googleapis.com/discovery/v1/apis/compute/v1/rest
        //

        private const string InstanceNamePattern = @"[a-z][-a-z0-9]{0,57}";
        private const string ZonePattern = @"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?";
        private const string ProjectIdPattern = @"(?:(?:[-a-z0-9]{1,63}\.)*(?:[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?):)?(?:[0-9]{1,19}|(?:[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?))";

        private static readonly Regex ZonalDnsPattern =
            new Regex($"^({InstanceNamePattern})\\.({ZonePattern})\\.c\\.({ProjectIdPattern})\\.internal$");

        private static readonly Regex GlobalDnsPattern =
            new Regex($"^({InstanceNamePattern})\\.c\\.({ProjectIdPattern})\\.internal$");

        protected InternalDnsName(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// DNS name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the DNS name.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }

        public static bool TryParse(string name, out InternalDnsName? result)
        {
            if (name == null)
            {
                result = null;
                return false;
            }

            var zonalMatch = ZonalDnsPattern.Match(name.ToLower());
            if (zonalMatch.Success)
            {
                result = new ZonalName(
                    name,
                    new InstanceLocator(
                        zonalMatch.Groups[3].Value,
                        zonalMatch.Groups[2].Value,
                        zonalMatch.Groups[1].Value));
                return true;
            }

            var globalMatch = GlobalDnsPattern.Match(name.ToLower());
            if (globalMatch.Success)
            {
                result = new GlobalName(name);
                return true;
            }

            result = null;
            return false;
        }

        public class ZonalName : InternalDnsName
        {
            internal ZonalName(
                string name,
                InstanceLocator instance) : base(name)
            {
                this.Instance = instance;
            }

            public ZonalName(InstanceLocator instance)
                : this(
                      $"{instance.Name}.{instance.Zone}.c.{instance.ProjectId}.internal",
                      instance)
            {
            }

            public InstanceLocator Instance { get; }
        }

        public class GlobalName : InternalDnsName
        {
            internal GlobalName(string name) : base(name)
            {
            }

            public GlobalName(InstanceLocator instance)
                : this($"{instance.Name}.c.{instance.ProjectId}.internal")
            {
            }
        }
    }
}
