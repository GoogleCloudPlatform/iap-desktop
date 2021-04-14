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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Application.Services.ProjectModel
{
    public interface IProjectModelNode
    {
        string DisplayName { get; }
    }

    public interface IProjectModelCloudNode : IProjectModelNode
    {
        IEnumerable<IProjectModelProjectNode> Projects { get; }
        IEnumerable<ProjectLocator> InaccessibleProjects { get; }
    }

    public interface IProjectModelProjectNode : IProjectModelNode
    {
        ProjectLocator Project { get; }
    }

    public interface IProjectModelZoneNode : IProjectModelNode
    {
        ZoneLocator Zone { get; }

        IEnumerable<IProjectModelInstanceNode> Instances { get; }
    }

    public interface IProjectModelInstanceNode : IProjectModelNode
    {
        ulong InstanceId { get; }

        InstanceLocator Instance { get; }

        bool IsRunning { get; }

        OperatingSystems OperatingSystem { get; }
    }
}
