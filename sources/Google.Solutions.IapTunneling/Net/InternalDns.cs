//
// Copyright 2019 Google LLC
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

using Google.Solutions.Common.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Net
{
    public static class InternalDns
    {
        //
        // "Official" patterns from discovers document
        // https://www.googleapis.com/discovery/v1/apis/compute/v1/rest
        //

        private const string InstanceNamePattern = @"[a-z][-a-z0-9]{0,57}";
        private const string ZonePattern = @"[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?";
        private const string ProjectIdPattern = @"(?:(?:[-a-z0-9]{1,63}\.)*(?:[a-z](?:[-a-z0-9]{0,61}[a-z0-9])?):)?(?:[0-9]{1,19}|(?:[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?))";

        private static readonly Regex ZonalDnsPattern =
            new Regex($"^({InstanceNamePattern})\\.({ZonePattern})\\.c\\.({ProjectIdPattern})\\.internal$");

        public static bool TryParseZonalDns(string name, out InstanceLocator locator)
        {
            var match = ZonalDnsPattern.Match(name.ToLower());
            if (match.Success)
            {
                locator = new InstanceLocator(
                    match.Groups[3].Value,
                    match.Groups[2].Value,
                    match.Groups[1].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }
    }
}
