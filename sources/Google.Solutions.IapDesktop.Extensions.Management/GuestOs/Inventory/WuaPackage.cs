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

using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA1056 // Uri properties should not be strings
#pragma warning disable CA1054 // Uri parameters should not be strings

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory
{
    public class WuaPackage : IPackage
    {
        [JsonProperty("Title")]
        public string Title { get; }

        [JsonProperty("Description")]
        public string Description { get; }

        [JsonProperty("Categories")]
        public IList<string> Categories { get; }

        [JsonProperty("CategoryIDs")]
        public IList<string> CategoryIDs { get; }

        [JsonProperty("KBArticleIDs")]
        public IList<string> KBArticleIDs { get; }

        [JsonProperty("SupportURL")]
        public string SupportURL { get; }

        [JsonProperty("UpdateID")]
        public string UpdateID { get; }

        [JsonProperty("RevisionNumber")]
        public int RevisionNumber { get; }

        [JsonProperty("LastDeploymentChangeTime")]
        public DateTime? LastDeploymentChangeTime { get; }

        //---------------------------------------------------------------------
        // IPackage
        //---------------------------------------------------------------------

        string IPackage.PackageId => this.Title;

        string? IPackage.Architecture => null;

        string IPackage.Version => this.RevisionNumber.ToString();

        Uri? IPackage.Weblink => !string.IsNullOrEmpty(this.SupportURL)
            ? new Uri(this.SupportURL)
            : null;

        DateTime? IPackage.InstalledOn => null;
        DateTime? IPackage.PublishedOn => this.LastDeploymentChangeTime;

        public PackageCriticality Criticality => WuaPackageType.MaxCriticality(this.CategoryIDs);

        public string PackageType =>
            string.Join(
                ", ",
                WuaPackageType
                    .FromCategoryIds(this.CategoryIDs)
                    .Select(t => t.Name));

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        [JsonConstructor]
        public WuaPackage(
            [JsonProperty("Title")] string title,
            [JsonProperty("Description")] string description,
            [JsonProperty("Categories")] IList<string> categories,
            [JsonProperty("CategoryIDs")] IList<string> categoryIDs,
            [JsonProperty("KBArticleIDs")] IList<string> kbArticleIDs,
            [JsonProperty("SupportURL")] string supportURL,
            [JsonProperty("UpdateID")] string updateID,
            [JsonProperty("RevisionNumber")] int revisionNumber,
            [JsonProperty("LastDeploymentChangeTime")] DateTime? lastDeploymentChangeTime)
        {
            this.Title = title;
            this.Description = description;
            this.Categories = categories;
            this.CategoryIDs = categoryIDs;
            this.KBArticleIDs = kbArticleIDs;
            this.SupportURL = supportURL;
            this.UpdateID = updateID;
            this.RevisionNumber = revisionNumber;
            this.LastDeploymentChangeTime = lastDeploymentChangeTime;
        }
    }

    internal class WuaPackageType
    {
        private static readonly IDictionary<Guid, WuaPackageType> Types
            = new Dictionary<Guid, WuaPackageType>()
            {
                // https://docs.microsoft.com/en-us/previous-versions/windows/desktop/ff357803(v=vs.85)
                { new Guid("5C9376AB-8CE6-464A-B136-22113DD69801"), new WuaPackageType("Application", PackageCriticality.NonCritical) },
                { new Guid("434DE588-ED14-48F5-8EED-A15E09A991F6"), new WuaPackageType("Connectors", PackageCriticality.NonCritical) },
                { new Guid("E6CF1350-C01B-414D-A61F-263D14D133B4"), new WuaPackageType("CriticalUpdates", PackageCriticality.Critical) },
                { new Guid("E0789628-CE08-4437-BE74-2495B842F43B"), new WuaPackageType("DefinitionUpdates", PackageCriticality.Critical) },
                { new Guid("E140075D-8433-45C3-AD87-E72345B36078"), new WuaPackageType("DeveloperKits", PackageCriticality.NonCritical) },
                { new Guid("B54E7D24-7ADD-428F-8B75-90A396FA584F"), new WuaPackageType("FeaturePacks", PackageCriticality.NonCritical) },
                { new Guid("9511D615-35B2-47BB-927F-F73D8E9260BB"), new WuaPackageType("Guidance", PackageCriticality.NonCritical) },
                { new Guid("0FA1201D-4330-4FA8-8AE9-B877473B6441"), new WuaPackageType("SecurityUpdates", PackageCriticality.Critical) },
                { new Guid("68C5B0A3-D1A6-4553-AE49-01D3A7827828"), new WuaPackageType("ServicePacks", PackageCriticality.NonCritical) },
                { new Guid("B4832BD8-E735-4761-8DAF-37F882276DAB"), new WuaPackageType("Tools", PackageCriticality.NonCritical) },
                { new Guid("28BC880E-0592-4CBF-8F95-C79B17911D5F"), new WuaPackageType("UpdateRollups", PackageCriticality.NonCritical) },
                { new Guid("CD5FFD1E-E932-4E3A-BF74-18BF0B1BBD83"), new WuaPackageType("Updates", PackageCriticality.NonCritical) }
            };

        public PackageCriticality Criticality { get; }
        public string Name { get; }

        private WuaPackageType(string name, PackageCriticality criticality)
        {
            this.Name = name;
            this.Criticality = criticality;
        }

        public static WuaPackageType FromCategoryId(string categoryId)
        {
            if (Guid.TryParse(categoryId, out var guid) &&
                Types.TryGetValue(guid, out var type))
            {
                return type;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<WuaPackageType> FromCategoryIds(IEnumerable<string> ids)
        {
            return ids
                .EnsureNotNull()
                .Select(id => FromCategoryId(id))
                .Where(type => type != null);
        }

        public static PackageCriticality MaxCriticality(IEnumerable<string> ids)
        {
            return FromCategoryIds(ids)
                .Select(type => type.Criticality)
                .ConcatItem(PackageCriticality.NonCritical) // Provide default in case enum is empty
                .Max();
        }
    }
}
