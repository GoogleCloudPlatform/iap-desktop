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

namespace Google.Solutions.Common.Locator
{

	
		public class DiskTypeLocator : ResourceLocator, IEquatable<DiskTypeLocator>
		{
            public string Zone { get; }
            public override string ResourceType => "diskTypes";
            public string Name => this.ResourceName;

		    public DiskTypeLocator(string projectId, string zone, string resourceName)
                : base(projectId, resourceName)
            {
                this.Zone = zone;
            }

            public static DiskTypeLocator FromString(string resourceReference)
            {
                resourceReference = StripUrlPrefix(resourceReference);

                // The resource name format is 
                // projects/[project-id]/zones/[zone]/[type]/[name]
                var parts = resourceReference.Split('/');
                if (parts.Length != 6 ||
                    string.IsNullOrEmpty(parts[1]) ||
                    string.IsNullOrEmpty(parts[3]) ||
                    string.IsNullOrEmpty(parts[4]) ||
                    string.IsNullOrEmpty(parts[5]) ||
                    parts[0] != "projects" ||
                    parts[2] != "zones" ||
                    parts[4] != "diskTypes")
                {
                    throw new ArgumentException($"'{resourceReference}' is not a valid zonal resource reference");
                }

                return new DiskTypeLocator(parts[1], parts[3], parts[5]);
            }

            public override int GetHashCode()
            {
                return
                    this.ProjectId.GetHashCode() ^
                    this.ResourceName.GetHashCode();
            }

            public override string ToString()
            {
                return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.ResourceName}";
            }

            public bool Equals(DiskTypeLocator other)
            {
                return other is object &&
                    this.ResourceName == other.ResourceName &&
                    this.Zone == other.Zone &&
                    this.ProjectId == other.ProjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is DiskTypeLocator locator && Equals(locator);
            }

            public static bool operator ==(DiskTypeLocator obj1, DiskTypeLocator obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(DiskTypeLocator obj1, DiskTypeLocator obj2)
            {
                return !(obj1 == obj2);
            }

		}

	
		public class InstanceLocator : ResourceLocator, IEquatable<InstanceLocator>
		{
            public string Zone { get; }
            public override string ResourceType => "instances";
            public string Name => this.ResourceName;

		    public InstanceLocator(string projectId, string zone, string resourceName)
                : base(projectId, resourceName)
            {
                this.Zone = zone;
            }

            public static InstanceLocator FromString(string resourceReference)
            {
                resourceReference = StripUrlPrefix(resourceReference);

                // The resource name format is 
                // projects/[project-id]/zones/[zone]/[type]/[name]
                var parts = resourceReference.Split('/');
                if (parts.Length != 6 ||
                    string.IsNullOrEmpty(parts[1]) ||
                    string.IsNullOrEmpty(parts[3]) ||
                    string.IsNullOrEmpty(parts[4]) ||
                    string.IsNullOrEmpty(parts[5]) ||
                    parts[0] != "projects" ||
                    parts[2] != "zones" ||
                    parts[4] != "instances")
                {
                    throw new ArgumentException($"'{resourceReference}' is not a valid zonal resource reference");
                }

                return new InstanceLocator(parts[1], parts[3], parts[5]);
            }

            public override int GetHashCode()
            {
                return
                    this.ProjectId.GetHashCode() ^
                    this.ResourceName.GetHashCode();
            }

            public override string ToString()
            {
                return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.ResourceName}";
            }

            public bool Equals(InstanceLocator other)
            {
                return other is object &&
                    this.ResourceName == other.ResourceName &&
                    this.Zone == other.Zone &&
                    this.ProjectId == other.ProjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is InstanceLocator locator && Equals(locator);
            }

            public static bool operator ==(InstanceLocator obj1, InstanceLocator obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(InstanceLocator obj1, InstanceLocator obj2)
            {
                return !(obj1 == obj2);
            }

		}

	}