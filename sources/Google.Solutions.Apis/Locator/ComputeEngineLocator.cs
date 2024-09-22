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

using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;

namespace Google.Solutions.Apis.Locator
{
    /// <summary>
    /// Locator for a Compute Engine resource.
    /// </summary>
    public abstract class ComputeEngineLocator : IEquatable<ComputeEngineLocator>
    {
        private const string ComputeGoogleapisPrefix = "https://compute.googleapis.com/compute/v1/";
        private const string GoogleapisUrlPrefix = "https://www.googleapis.com/compute/v1/";

        /// <summary>
        /// ID if project that the resource belongs to.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// Type of resource.
        /// </summary>
        public abstract string ResourceType { get; }

        /// <summary>
        /// User-provided unique name or ID of resource.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Project that the resource belongs to.
        /// </summary>
        public ProjectLocator Project => new ProjectLocator(this.ProjectId);

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

        protected ComputeEngineLocator(
            string projectId,
            string resourceName)
        {
            Precondition.ExpectNotNull(projectId, nameof(projectId));
            Precondition.ExpectNotNull(resourceName, nameof(resourceName));

            Debug.Assert(!long.TryParse(projectId, out var _));
            Debug.Assert(!long.TryParse(resourceName, out var _));
            Debug.Assert(!projectId.Contains("/"));

            this.ProjectId = projectId;
            this.Name = resourceName;
        }

        public abstract bool Equals(ComputeEngineLocator? other);
    }
}
