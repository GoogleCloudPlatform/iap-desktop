//
// Copyright 2020 Google LLC
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

#pragma warning disable CA1507 // Use nameof to express symbol names

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory
{
    public class Package : IPackage
    {
        [JsonProperty("Name")]
        public string? Name { get; }

        [JsonProperty("Arch")]
        public string? Architecture { get; }

        [JsonProperty("Version")]
        public string? Version { get; }

        //---------------------------------------------------------------------
        // IPackage
        //---------------------------------------------------------------------

        string? IPackage.PackageId => this.Name;

        string? IPackage.Description => null;

        DateTime? IPackage.InstalledOn => null;

        DateTime? IPackage.PublishedOn => null;

        Uri? IPackage.Weblink => null;

        PackageCriticality IPackage.Criticality => PackageCriticality.NonCritical;

        string IPackage.PackageType => "Package";

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        [JsonConstructor]
        public Package(
            [JsonProperty("Name")] string? name,
            [JsonProperty("Arch")] string? arch,
            [JsonProperty("Version")] string? version)
        {
            this.Name = name;
            this.Architecture = arch;
            this.Version = version;
        }
    }
}
