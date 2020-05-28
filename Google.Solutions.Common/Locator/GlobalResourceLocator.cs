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

	
		public class ImageLocator : ResourceLocator, IEquatable<ImageLocator>
		{
            public override string ResourceType => "images";
            public string Name => this.ResourceName;

		    public ImageLocator(string projectId, string resourceName)
                : base(projectId, resourceName)
            {
            }

            public static ImageLocator FromString(string resourceReference)
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
                    parts[2] != "global" ||
                    parts[3] != "images")
                {
                    throw new ArgumentException($"'{resourceReference}' is not a valid global resource reference");
                }

                return new ImageLocator(
                    parts[1],
                    string.Join("/", parts.Skip(4)));
            }

            public override int GetHashCode()
            {
                return
                    this.ProjectId.GetHashCode() ^
                    this.ResourceName.GetHashCode();
            }

            public override string ToString()
            {
                return $"projects/{this.ProjectId}/global/{this.ResourceType}/{this.ResourceName}";
            }

            public bool Equals(ImageLocator other)
            {
                return other is object &&
                    this.ResourceName == other.ResourceName &&
                    this.ProjectId == other.ProjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is ImageLocator locator && Equals(locator);
            }

            public static bool operator ==(ImageLocator obj1, ImageLocator obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(ImageLocator obj1, ImageLocator obj2)
            {
                return !(obj1 == obj2);
            }

		}

	
		public class LicenseLocator : ResourceLocator, IEquatable<LicenseLocator>
		{
            public override string ResourceType => "licenses";
            public string Name => this.ResourceName;

		    public LicenseLocator(string projectId, string resourceName)
                : base(projectId, resourceName)
            {
            }

            public static LicenseLocator FromString(string resourceReference)
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
                    parts[2] != "global" ||
                    parts[3] != "licenses")
                {
                    throw new ArgumentException($"'{resourceReference}' is not a valid global resource reference");
                }

                return new LicenseLocator(
                    parts[1],
                    string.Join("/", parts.Skip(4)));
            }

            public override int GetHashCode()
            {
                return
                    this.ProjectId.GetHashCode() ^
                    this.ResourceName.GetHashCode();
            }

            public override string ToString()
            {
                return $"projects/{this.ProjectId}/global/{this.ResourceType}/{this.ResourceName}";
            }

            public bool Equals(LicenseLocator other)
            {
                return other is object &&
                    this.ResourceName == other.ResourceName &&
                    this.ProjectId == other.ProjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is LicenseLocator locator && Equals(locator);
            }

            public static bool operator ==(LicenseLocator obj1, LicenseLocator obj2)
            {
                if (obj1 is null)
                {
                    return obj2 is null;
                }

                return obj1.Equals(obj2);
            }

            public static bool operator !=(LicenseLocator obj1, LicenseLocator obj2)
            {
                return !(obj1 == obj2);
            }

		}

	}