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

using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace Google.Solutions.Apis.Locator
{

    public class ImageLocator : ResourceLocator, IEquatable<ImageLocator>
    {
        public override string ResourceType => "images";

        [JsonConstructor]
        public ImageLocator(string projectId, string name)
            : base(projectId, name)
        {
        }

        public ImageLocator(ProjectLocator project, string name)
            : base(project.ProjectId, name)
        {
        }

        public static ImageLocator Parse(string resourceReference)
        {
            resourceReference = StripUrlPrefix(resourceReference);

            var match = new Regex("(?:/compute/beta/)?projects/(.*)/global/images/(.*)")
                .Match(resourceReference);
            if (match.Success)
            {
                return new ImageLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
            }
            else
            {
                throw new ArgumentException($"'{resourceReference}' is not a valid global resource reference");
            }
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"projects/{this.ProjectId}/global/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(ImageLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ResourceLocator? other)
        {
            return other is ImageLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is ImageLocator locator && Equals(locator);
        }

        public static bool operator ==(ImageLocator? obj1, ImageLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ImageLocator? obj1, ImageLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }


    public class LicenseLocator : ResourceLocator, IEquatable<LicenseLocator>
    {
        public override string ResourceType => "licenses";

        [JsonConstructor]
        public LicenseLocator(string projectId, string name)
            : base(projectId, name)
        {
        }

        public LicenseLocator(ProjectLocator project, string name)
            : base(project.ProjectId, name)
        {
        }

        public static LicenseLocator Parse(string resourceReference)
        {
            resourceReference = StripUrlPrefix(resourceReference);

            var match = new Regex("(?:/compute/beta/)?projects/(.*)/global/licenses/(.*)")
                .Match(resourceReference);
            if (match.Success)
            {
                return new LicenseLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value);
            }
            else
            {
                throw new ArgumentException($"'{resourceReference}' is not a valid global resource reference");
            }
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"projects/{this.ProjectId}/global/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(LicenseLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ResourceLocator? other)
        {
            return other is LicenseLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is LicenseLocator locator && Equals(locator);
        }

        public static bool operator ==(LicenseLocator? obj1, LicenseLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(LicenseLocator? obj1, LicenseLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }

}