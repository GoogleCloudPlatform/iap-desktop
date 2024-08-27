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

using Google.Solutions.IapDesktop.Extensions.Management.Properties;
using Google.Solutions.Mvvm.Controls;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.PackageInventory
{
    internal class PackageList : SearchableList<PackageInventoryModel.Item>
    {
        public PackageList()
        {
            this.List.SmallImageList = new ImageList();
            this.List.SmallImageList.Images.Add(Resources.Package_16);
            this.List.SmallImageList.Images.Add(Resources.PackageWarn_16);

            AddColumn("Type", 120);
            AddColumn("Package", 300);
            AddColumn("Version", 80);
            AddColumn("Architecture", 80);
            AddColumn("Instance name", 130);
            AddColumn("Zone", 80);
            AddColumn("Project ID", 120);
            AddColumn("Published", 90);
            AddColumn("Installed", 90);
            AddColumn("Description", 250);

            this.List.GridLines = true;

            this.List.BindImageIndex(m => (int)m.Package.Criticality);
            this.List.BindColumn(0, m => m.Package.PackageType);
            this.List.BindColumn(1, m => m.Package.PackageId ?? string.Empty);
            this.List.BindColumn(2, m => m.Package.Version ?? string.Empty);
            this.List.BindColumn(3, m => m.Package.Architecture ?? string.Empty);
            this.List.BindColumn(4, m => m.Instance.Name);
            this.List.BindColumn(5, m => m.Instance.Zone);
            this.List.BindColumn(6, m => m.Instance.ProjectId);
            this.List.BindColumn(7, m => m.Package.PublishedOn?.ToShortDateString() ?? string.Empty);
            this.List.BindColumn(8, m => m.Package.InstalledOn?.ToShortDateString() ?? string.Empty);
            this.List.BindColumn(9, m => m.Package.Description ?? string.Empty);
        }
    }
}
