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

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory
{
    public class QfePackage : IPackage
    {
        [JsonProperty("Caption")]
        public string Caption { get; }

        [JsonProperty("Description")]
        public string Description { get; }

        [JsonProperty("HotFixID")]
        public string HotFixID { get; }

        [JsonProperty("InstalledOn")]
        public DateTime? InstalledOn { get; }

        //---------------------------------------------------------------------
        // IPackage
        //---------------------------------------------------------------------

        string IPackage.PackageId => this.HotFixID;

        string? IPackage.Architecture => null;

        string? IPackage.Version => null;

        Uri? IPackage.Weblink
        {
            get
            {
                if (this.Caption != null && Uri.TryCreate(
                    this.Caption, UriKind
                    .Absolute,
                    out var uri))
                {
                    return uri;
                }
                else
                {
                    return null;
                }
            }
        }

        PackageCriticality IPackage.Criticality => PackageCriticality.NonCritical;

        string IPackage.PackageType => "Hotfix";
        DateTime? IPackage.PublishedOn => null;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        [JsonConstructor]
        public QfePackage(
            [JsonProperty("Caption")] string caption,
            [JsonProperty("Description")] string description,
            [JsonProperty("HotFixID")] string hotFixID,
            [JsonProperty("InstalledOn")] DateTime? installedOn)
        {
            this.Caption = caption;
            this.Description = description;
            this.HotFixID = hotFixID;

            if (installedOn != null)
            {
                // For some reason, installedOn is not provided in ISO
                // format, so it lacks a time zone specification.
                this.InstalledOn = DateTime.SpecifyKind(
                    installedOn.Value,
                    DateTimeKind.Utc);
            }
        }
    }
}
