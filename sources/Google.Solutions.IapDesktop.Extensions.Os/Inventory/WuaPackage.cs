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
using System.Linq;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Extensions.Os.Inventory
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

        string IPackage.PackageId => this.KBArticleIDs.FirstOrDefault();

        string IPackage.Architecture => null;

        string IPackage.Version => this.RevisionNumber.ToString();

        Uri IPackage.Weblink => !string.IsNullOrEmpty(this.SupportURL)
            ? new Uri(this.SupportURL)
            : null;

        DateTime? IPackage.InstalledOn => this.LastDeploymentChangeTime;
        
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
}
