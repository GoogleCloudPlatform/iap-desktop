//
// Copyright 2024 Google LLC
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

using Google.Solutions.Apis.Locator;

namespace Google.Solutions.IapDesktop.Core.EntityModel.Resources
{
    /// <summary>
    /// A Google Cloud project.
    /// </summary>
    public class Project : IEntity<ProjectLocator>
    {
        public Project(
            OrganizationLocator organizationLocator,
            ProjectLocator locator, 
            string projectName,
            bool accessible)
        {
            this.Organization = organizationLocator;
            this.DisplayName = projectName;
            this.Locator = locator;
            this.IsAccessible = accessible;
        }

        /// <summary>
        /// Indicates whether this project can be accessed
        /// by the current user.
        /// </summary>
        public bool IsAccessible { get; }

        public OrganizationLocator Organization { get; }

        //----------------------------------------------------------------------
        // IEntity.
        //----------------------------------------------------------------------

        public string DisplayName { get; }

        public ProjectLocator Locator { get; }
    }
}
