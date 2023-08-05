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
    /// An identity within a workforce pool, identified by a principal identifier.
    /// </summary>
    internal class WorkforcePoolIdentity
    {
        public WorkforcePoolIdentity(string location, string pool, string subject)
        {
            this.Location = location.ExpectNotEmpty(nameof(location));
            this.Pool = pool.ExpectNotEmpty(nameof(pool));
            this.Subject = subject.ExpectNotEmpty(nameof(subject));
        }

        /// <summary>
        /// Location, typically 'global'.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// ID of the workforce pool.
        /// </summary>
        public string Pool { get; }

        /// <summary>
        /// Value of 'google.subject' as defined by the attribute mapping.
        /// The subject may be an email address, but it can also be an opaque ID
        /// such as a GUID.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Parse a principal idnentifier in the format
        /// 
        /// principal://iam.googleapis.com/locations/LOCATION/workforcePools/POOL/subject/SUBJECT"
        /// </summary>
        public static WorkforcePoolIdentity FromPrincipalIdentifier(string principalIdentifier)
        {
            principalIdentifier.ExpectNotEmpty(nameof(principalIdentifier));

            var match = new Regex(
                "^principal://iam.googleapis.com/locations/(.*)/workforcePools/(.*)/subject/(.*)$")
                .Match(principalIdentifier.Trim());
            if (match.Success)
            {
                return new WorkforcePoolIdentity(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
            }
            else
            {
                throw new ArgumentException(
                    $"The prinipcal '{principalIdentifier}' is not a Workforce Identity subject");
            }
        }

        public override string ToString()
        {
            return "principal://iam.googleapis.com/locations/" +
                $"{this.Location}/workforcePools/{this.Pool}/subject/{this.Subject}";
        }
    }
}
