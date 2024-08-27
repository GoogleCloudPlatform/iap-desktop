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
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory
{
    public class GuestPackages
    {
        [JsonProperty("yum")]
        public IList<Package> YumPackages { get; }

        [JsonProperty("rpm")]
        public IList<Package> RpmPackages { get; }

        [JsonProperty("apt")]
        public IList<Package> AptPackages { get; }

        [JsonProperty("deb")]
        public IList<Package> DebPackages { get; }

        [JsonProperty("zypper")]
        public IList<Package> ZypperPackages { get; }

        [JsonProperty("zypperPatches")]
        public IList<ZypperPatch> ZypperPatches { get; }

        [JsonProperty("gem")]
        public IList<Package> GemPackages { get; }

        [JsonProperty("pip")]
        public IList<Package> PipPackages { get; }

        [JsonProperty("googet")]
        public IList<Package> GoogetPackages { get; }

        [JsonProperty("wua")]
        public IList<WuaPackage> WuaPackages { get; }

        [JsonProperty("qfe")]
        public IList<QfePackage> QfePackages { get; }

        public IEnumerable<IPackage> AllPackages =>
            Enumerable.Empty<IPackage>()
                .Concat(this.YumPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.RpmPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.AptPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.DebPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.ZypperPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.GemPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.PipPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.GoogetPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.WuaPackages.EnsureNotNull().Cast<IPackage>())
                .Concat(this.QfePackages.EnsureNotNull().Cast<IPackage>());

        [JsonConstructor]
        public GuestPackages(
            [JsonProperty("yum")] IList<Package>? yumPackages,
            [JsonProperty("rpm")] IList<Package>? rpmPackages,
            [JsonProperty("apt")] IList<Package>? aptPackages,
            [JsonProperty("deb")] IList<Package>? debPackages,
            [JsonProperty("zypper")] IList<Package>? zypperPackages,
            [JsonProperty("zypperPatches")] IList<ZypperPatch>? zypperPatches,
            [JsonProperty("gem")] IList<Package>? gemPackages,
            [JsonProperty("pip")] IList<Package>? pipPackages,
            [JsonProperty("googet")] IList<Package>? googetPackages,
            [JsonProperty("wua")] IList<WuaPackage>? wuaPackages,
            [JsonProperty("qfe")] IList<QfePackage>? qfePackages)
        {
            this.YumPackages = yumPackages ?? new List<Package>();
            this.RpmPackages = rpmPackages ?? new List<Package>();
            this.AptPackages = aptPackages ?? new List<Package>();
            this.DebPackages = debPackages ?? new List<Package>();
            this.ZypperPackages = zypperPackages ?? new List<Package>();
            this.ZypperPatches = zypperPatches ?? new List<ZypperPatch>();
            this.GemPackages = gemPackages ?? new List<Package>();
            this.PipPackages = pipPackages ?? new List<Package>();
            this.GoogetPackages = googetPackages ?? new List<Package>();
            this.WuaPackages = wuaPackages ?? new List<WuaPackage>();
            this.QfePackages = qfePackages ?? new List<QfePackage>();
        }
    }
}
