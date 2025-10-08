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

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{
    /// <summary>
    /// A Google Cloud organization.
    /// </summary>
    public class Organization : IResource<OrganizationLocator>
    {
        /// <summary>
        /// Default organzization, used in places where the actual
        /// organization is unknown or inaccessible.
        /// </summary>
        public static readonly Organization Default = new Organization(
            new OrganizationLocator(0),
            "Default organization");

        public Organization(OrganizationLocator locator, string displayName)
        {
            this.DisplayName = displayName;
            this.Locator = locator;
        }

        //----------------------------------------------------------------------
        // IEntity.
        //----------------------------------------------------------------------

        /// <summary>
        /// Primary domain of the organization, or a different display name.
        /// </summary>
        public string DisplayName { get; }

        public OrganizationLocator Locator { get; }
    }
}
