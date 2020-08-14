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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Extensions.Os.Inventory;
using Google.Solutions.IapDesktop.Extensions.Os.Properties;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory
{
    internal class PackageList : SearchableList<PackageInventoryModel.Item>
    {
        public PackageList()
        {
            this.List.SmallImageList = new ImageList();
            this.List.SmallImageList.Images.Add(Resources.Package_16);

            AddColumn("Instance name", 130);
            AddColumn("Zone", 80);
            AddColumn("Project ID", 120);
            AddColumn("Package", 150);
            AddColumn("Version", 80);
            AddColumn("Architecture", 120);
            AddColumn("Description", 200);
            AddColumn("Installed (UTC)", 120);

            this.List.BindImageIndex(_ => 0);
            this.List.BindColumn(0, m => m.Instance.Name);
            this.List.BindColumn(1, m => m.Instance.Zone);
            this.List.BindColumn(2, m => m.Instance.ProjectId);
            this.List.BindColumn(2, m => m.Package.PackageId);
            this.List.BindColumn(2, m => m.Package.Version);
            this.List.BindColumn(2, m => m.Package.Architecture);
            this.List.BindColumn(2, m => m.Package.Description);
            this.List.BindColumn(2, m => m.Package.InstalledOn.ToString());
        }
    }
}
