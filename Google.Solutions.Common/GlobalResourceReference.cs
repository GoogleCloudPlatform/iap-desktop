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

using System;
using System.Linq;
using System.Diagnostics;

namespace Google.Solutions.Common
{
    /// <summary>
    /// Represents a resource reference as used by the GCE APIs:
    /// 
    /// projects/[project-id]/global/[type]/[name]
    /// </summary>
    public class GlobalResourceReference : ResourceReference, IEquatable<GlobalResourceReference>
    {
        public GlobalResourceReference(string projectId, string resourceType, string resourceName)
            : base(projectId, resourceType, resourceName)
        {
        }

        public static GlobalResourceReference FromString(string resourceReference)
        {
            resourceReference = StripUrlPrefix(resourceReference);

            // The resource name format is 
            // projects/[project-id]/global/[type]/[name], but
            // [name] might contain slashes.
            var parts = resourceReference.Split('/');
            if (parts.Length < 5 ||
                string.IsNullOrEmpty(parts[1]) ||
                string.IsNullOrEmpty(parts[3]) ||
                string.IsNullOrEmpty(parts[4]) ||
                parts[0] != "projects" ||
                parts[2] != "global")
            {
                throw new ArgumentException($"'{resourceReference}' is not a valid global resource reference");
            }

            return new GlobalResourceReference(
                parts[1], 
                parts[3], 
                string.Join("/", parts.Skip(4)));
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.ResourceType.GetHashCode() ^
                this.ResourceName.GetHashCode();
        }

        public override string ToString()
        {
            return $"projects/{this.ProjectId}/global/{this.ResourceType}/{this.ResourceName}";
        }

        public bool Equals(GlobalResourceReference other)
        {
            return !object.ReferenceEquals(other, null) &&
                this.ResourceType == other.ResourceType &&
                this.ResourceName == other.ResourceName &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object obj)
        {
            return obj is GlobalResourceReference &&
                Equals((GlobalResourceReference)obj);
        }

        public static bool operator ==(GlobalResourceReference obj1, GlobalResourceReference obj2)
        {
            if (object.ReferenceEquals(obj1, null))
            {
                return object.ReferenceEquals(obj2, null);
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(GlobalResourceReference obj1, GlobalResourceReference obj2)
        {
            return !(obj1 == obj2);
        }
    }
}

