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
using System.IO;
using System.Text.RegularExpressions;

namespace Google.Solutions.Apis.Locator
{
    /// <summary>
    /// Locator for projects. These locators are global, but differ from
    /// other global locators by not having a "global/" part in the URL.
    /// </summary>
    public class ProjectLocator : ComputeEngineLocator, IEquatable<ProjectLocator>
    {
        public override string ResourceType => "projects";

        public ProjectLocator(string projectId)
            : base(projectId, projectId)
        {
        }

        public static bool TryParse(string path, out ProjectLocator? locator)
        {
            path = StripUrlPrefix(path);

            var match = new Regex("(?:/compute/beta/)?projects/([^/]*)$")
                .Match(path);
            if (match.Success)
            {
                locator = new ProjectLocator(match.Groups[1].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static ProjectLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid project locator");
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
            return $"projects/{this.ProjectId}";
        }

        public bool Equals(ProjectLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ComputeEngineLocator? other)
        {
            return other is ProjectLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is ProjectLocator locator && Equals(locator);
        }

        public static bool operator ==(ProjectLocator? obj1, ProjectLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ProjectLocator? obj1, ProjectLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
