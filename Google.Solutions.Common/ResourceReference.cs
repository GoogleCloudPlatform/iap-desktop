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

using Google.Apis.Util;
using System.Diagnostics;

namespace Google.Solutions.Common
{
    public abstract class ResourceReference
    {
        private const string ComputeGoogleapisPrefix = "https://compute.googleapis.com/compute/v1/";
        private const string GoogleapisUrlPrefix = "https://www.googleapis.com/compute/v1/";

        public string ProjectId { get; }
        public string ResourceType { get; }
        public string ResourceName { get; }

        protected static string StripUrlPrefix(string resourceReference)
        {
            if (resourceReference.StartsWith(ComputeGoogleapisPrefix))
            {
                return resourceReference.Substring(ComputeGoogleapisPrefix.Length);
            }
            else if (resourceReference.StartsWith(GoogleapisUrlPrefix))
            {
                return resourceReference.Substring(GoogleapisUrlPrefix.Length);
            }
            else
            {
                return resourceReference;
            }
        }

        protected ResourceReference(
            string projectId,
            string resourceType,
            string resourceName)
        {
            Utilities.ThrowIfNull(projectId, nameof(projectId));
            Utilities.ThrowIfNull(resourceType, nameof(resourceType));
            Utilities.ThrowIfNull(resourceName, nameof(resourceName));

            Debug.Assert(!long.TryParse(projectId, out long _));
            Debug.Assert(!long.TryParse(resourceName, out long _));
            Debug.Assert(!projectId.Contains("/"));

            this.ProjectId = projectId;
            this.ResourceType = resourceType;
            this.ResourceName = resourceName;
        }
    }
}
