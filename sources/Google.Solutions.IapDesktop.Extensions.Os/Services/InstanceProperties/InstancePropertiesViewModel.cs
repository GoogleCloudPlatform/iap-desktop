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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Services.Windows.Properties;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Os.Services.InstanceProperties
{
    public class InstancePropertiesViewModel : ViewModelBase, IPropertiesViewModel
    {
        public bool IsInformationBarVisible { get; private set; }
        public string InformationText { get; private set; }
        public object InspectedObject { get; private set; }

        public void SaveChanges()
        {
            Debug.Assert(
                false,
                "All properties are read-only, so this should never be called");
        }

        public Task SwitchToModelAsync(IProjectExplorerNode node)
        {
            throw new NotImplementedException();
        }
    }
}
