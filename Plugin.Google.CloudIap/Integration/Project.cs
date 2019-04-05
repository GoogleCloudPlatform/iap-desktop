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

using Google;
using Google.Apis.Compute.v1.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Google.CloudIap.Integration
{
    /// <summary>
    /// Represents a GCP project.
    /// </summary>
    internal class Project
    {
        private readonly string projectId;
        private readonly IEnumerable<string> zones;
        private readonly ComputeEngineAdapter adapter;

        public Project(string projectId, IEnumerable<string> zones, ComputeEngineAdapter adapter)
        {
            this.projectId = projectId;
            this.Zones = zones;
            this.adapter = adapter;
        }

        public IEnumerable<string> Zones { get; private set; }

        public async Task<IEnumerable<Instance>> QueryInstancesAcrossZones()
        {
            var queries = new List<Task<IEnumerable<Instance>>>();
            foreach (var zone in this.Zones)
            {
                queries.Add(this.adapter.QueryInstancesAsync(this.projectId, zone));
            }

            try
            {
                await Task.WhenAll(queries);
            }
            catch (GoogleApiException)
            {
                // Ignore for now, will handle errors below.
            }

            var instances = new List<Instance>();
            foreach (var query in queries)
            {
                if (query.IsFaulted && 
                    query.Exception.Message.Contains("Invalid value for zone"))
                {
                    // Inaccessible zone, ignore.
                }
                else
                {
                    instances.AddRange(query.Result);
                }
            }
            return instances;
        }

        public ComputeEngineAdapter.SerialPortStream GetSerialPortOutput(VmInstanceReference instanceRef)
        {
            return this.adapter.GetSerialPortOutput(instanceRef);
        }

        public Task<Instance> QueryInstanceAsync(VmInstanceReference instanceRef)
        {
            return this.adapter.QueryInstanceAsync(instanceRef.ProjectId, instanceRef.Zone, instanceRef.InstanceName);
        }
    }

    /// <summary>
    /// Represents a set of GCP projects.
    /// </summary>
    internal class ProjectCollection
    {
        private readonly ComputeEngineAdapter adapter;

        public ProjectCollection(ComputeEngineAdapter adapter)
        {
            this.adapter = adapter;
        }

        public async Task<Project> GetProjectAsync(string projectId)
        {
            // Eagerly query zones. We will need these later anyway, and it helps
            // validating that the projectId exists.
            try
            {
                var zones = await this.adapter.QueryZonesAsync(projectId);
                return new Project(projectId, zones, this.adapter);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Cannot load project {projectId}", e);
            }
        }
    }
}
