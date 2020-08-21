﻿//
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

namespace Google.Solutions.IapDesktop.Extensions.Os.Inventory
{
    public class ZypperPatch : IPackage
    {
        [JsonProperty("Name")]
        public string Name { get; }

        [JsonProperty("Category")]
        public string Category { get; }

        [JsonProperty("Severity")]
        public string Severity { get; }

        [JsonProperty("Summary ")]
        public string Summary { get; }


        //---------------------------------------------------------------------
        // IPackage
        //---------------------------------------------------------------------

        string IPackage.PackageId => this.Name;

        string IPackage.Architecture => null;

        string IPackage.Version => null;

        Uri IPackage.Weblink => null;

        string IPackage.Description => this.Summary +
            (this.Category != null ? $" ({this.Category})" : string.Empty);

        DateTime? IPackage.InstalledOn => null;

        PackageCriticality IPackage.Criticality => PackageCriticality.NonCritical;

        string IPackage.PackageType => "Patch";

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        [JsonConstructor]
        public ZypperPatch(
            [JsonProperty("Name")] string name,
            [JsonProperty("Category")] string category,
            [JsonProperty("Severity")] string severity,
            [JsonProperty("Summary")] string summary)
        {
            this.Name = name;
            this.Category = category;
            this.Severity = severity;
            this.Summary = summary;
        }
    }
}
