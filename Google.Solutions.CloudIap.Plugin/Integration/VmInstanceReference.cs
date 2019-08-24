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

using System;

namespace Google.Solutions.CloudIap.Plugin.Integration
{
    /// <summary>
    /// Compound identifier to a VM instancem suitable to be used
    /// as key in a dictionary.
    /// </summary>
    internal class VmInstanceReference : IEquatable<VmInstanceReference>
    {
        public string ProjectId { get; private set; }
        public string Zone{ get; private set; }
        public string InstanceName { get; private set; }

        public VmInstanceReference(string projectId, string zone, string instanceName)
        {
            this.ProjectId = projectId;
            this.Zone = zone;
            this.InstanceName = instanceName;
        }

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.Zone.GetHashCode() ^
                this.InstanceName.GetHashCode();
        }

        public override string ToString()
        {
            return this.InstanceName;
        }

        public bool Equals(VmInstanceReference other)
        {
            return other != null && 
                this.InstanceName == other.InstanceName &&
                this.Zone == other.Zone &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object obj)
        {
            return obj is VmInstanceReference &&
                Equals((VmInstanceReference)obj);
        }

        // TODO: operator ==, !=
    }
}
