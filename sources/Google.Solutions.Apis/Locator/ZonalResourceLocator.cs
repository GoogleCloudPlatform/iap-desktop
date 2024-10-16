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

    public class DiskTypeLocator : ComputeEngineLocator, IEquatable<DiskTypeLocator>
    {
        public string Zone { get; }
        public override string ResourceType => "diskTypes";

        [JsonConstructor]
        public DiskTypeLocator(string projectId, string zone, string name)
            : base(projectId, name)
        {
            this.Zone = zone;
        }

        public DiskTypeLocator(ProjectLocator project, string zone, string name)
            : this(project.ProjectId, zone, name)
        {
        }

        public static bool TryParse(string path, out DiskTypeLocator? locator)
        {
            path = StripUrlPrefix(path);

            var match = new Regex("(?:/compute/beta/)?projects/(.+)/zones/(.+)/diskTypes/(.+)")
                .Match(path);
            if (match.Success)
            {
                locator = new DiskTypeLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static DiskTypeLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid zonal resource locator");
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
            return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(DiskTypeLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.Zone == other.Zone &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ComputeEngineLocator? other)
        {
            return other is DiskTypeLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is DiskTypeLocator locator && Equals(locator);
        }

        public static bool operator ==(DiskTypeLocator? obj1, DiskTypeLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(DiskTypeLocator? obj1, DiskTypeLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }


    public class InstanceLocator : ComputeEngineLocator, IEquatable<InstanceLocator>
    {
        public string Zone { get; }
        public override string ResourceType => "instances";

        [JsonConstructor]
        public InstanceLocator(string projectId, string zone, string name)
            : base(projectId, name)
        {
            this.Zone = zone;
        }

        public InstanceLocator(ProjectLocator project, string zone, string name)
            : this(project.ProjectId, zone, name)
        {
        }

        public static bool TryParse(string path, out InstanceLocator? locator)
        {
            path = StripUrlPrefix(path);

            var match = new Regex("(?:/compute/beta/)?projects/(.+)/zones/(.+)/instances/(.+)")
                .Match(path);
            if (match.Success)
            {
                locator = new InstanceLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static InstanceLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid zonal resource locator");
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
            return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(InstanceLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.Zone == other.Zone &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ComputeEngineLocator? other)
        {
            return other is InstanceLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is InstanceLocator locator && Equals(locator);
        }

        public static bool operator ==(InstanceLocator? obj1, InstanceLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(InstanceLocator? obj1, InstanceLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }


    public class MachineTypeLocator : ComputeEngineLocator, IEquatable<MachineTypeLocator>
    {
        public string Zone { get; }
        public override string ResourceType => "machineTypes";

        [JsonConstructor]
        public MachineTypeLocator(string projectId, string zone, string name)
            : base(projectId, name)
        {
            this.Zone = zone;
        }

        public MachineTypeLocator(ProjectLocator project, string zone, string name)
            : this(project.ProjectId, zone, name)
        {
        }

        public static bool TryParse(string path, out MachineTypeLocator? locator)
        {
            path = StripUrlPrefix(path);

            var match = new Regex("(?:/compute/beta/)?projects/(.+)/zones/(.+)/machineTypes/(.+)")
                .Match(path);
            if (match.Success)
            {
                locator = new MachineTypeLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static MachineTypeLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid zonal resource locator");
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
            return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(MachineTypeLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.Zone == other.Zone &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ComputeEngineLocator? other)
        {
            return other is MachineTypeLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is MachineTypeLocator locator && Equals(locator);
        }

        public static bool operator ==(MachineTypeLocator? obj1, MachineTypeLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(MachineTypeLocator? obj1, MachineTypeLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }


    public class NodeTypeLocator : ComputeEngineLocator, IEquatable<NodeTypeLocator>
    {
        public string Zone { get; }
        public override string ResourceType => "nodeTypes";

        [JsonConstructor]
        public NodeTypeLocator(string projectId, string zone, string name)
            : base(projectId, name)
        {
            this.Zone = zone;
        }

        public NodeTypeLocator(ProjectLocator project, string zone, string name)
            : this(project.ProjectId, zone, name)
        {
        }

        public static bool TryParse(string path, out NodeTypeLocator? locator)
        {
            path = StripUrlPrefix(path);

            var match = new Regex("(?:/compute/beta/)?projects/(.+)/zones/(.+)/nodeTypes/(.+)")
                .Match(path);
            if (match.Success)
            {
                locator = new NodeTypeLocator(
                    match.Groups[1].Value,
                    match.Groups[2].Value,
                    match.Groups[3].Value);
                return true;
            }
            else
            {
                locator = null;
                return false;
            }
        }

        public static NodeTypeLocator Parse(string path)
        {
            if (TryParse(path, out var locator))
            {
                return locator!;
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid zonal resource locator");
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
            return $"projects/{this.ProjectId}/zones/{this.Zone}/{this.ResourceType}/{this.Name}";
        }

        public bool Equals(NodeTypeLocator? other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.Zone == other.Zone &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(ComputeEngineLocator? other)
        {
            return other is NodeTypeLocator locator && Equals(locator);
        }

        public override bool Equals(object? obj)
        {
            return obj is NodeTypeLocator locator && Equals(locator);
        }

        public static bool operator ==(NodeTypeLocator? obj1, NodeTypeLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(NodeTypeLocator? obj1, NodeTypeLocator? obj2)
        {
            return !(obj1 == obj2);
        }

    }

}