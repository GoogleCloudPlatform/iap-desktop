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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Platform.Net;
using System;
using System.Net;

#pragma warning disable CA1822 // Mark members as static

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public interface ICloudConsoleAdapter
    {
        void ConfigureIapAccess(string projectId);
        void OpenInstanceDetails(InstanceLocator instance);
        void OpenInstanceList(ProjectLocator project);
        void OpenInstanceList(ZoneLocator zone);
        void OpenLogs(IProjectModelNode node);
        void OpenMyAccount();
        void OpenVmInstanceLogDetails(string projectId, string insertId, DateTime timestamp);
    }

    [SkipCodeCoverage("UI code")]
    public class CloudConsoleAdapter : ICloudConsoleAdapter
    {
        public void OpenInstanceDetails(InstanceLocator instance)
        {
            Browser.Default.Navigate(
                "https://console.cloud.google.com/compute/instancesDetail/zones/" +
                $"{instance.Zone}/instances/{instance.Name}?project={instance.ProjectId}");
        }

        public void OpenInstanceList(ProjectLocator project)
        {
            Browser.Default.Navigate(
                "https://console.cloud.google.com/compute/instances" +
                $"?project={project.ProjectId}");
        }

        public void OpenInstanceList(ZoneLocator zone)
        {
            var query = "[{\"k\":\"zoneForFilter\",\"v\":\"" + zone.Name + "\"}]";
            Browser.Default.Navigate(
                "https://console.cloud.google.com/compute/instances" +
                $"?project={zone.ProjectId}&instancesquery={WebUtility.UrlEncode(query)}");
        }

        private void OpenLogs(string projectId, string query)
        {
            Browser.Default.Navigate(
                "https://console.cloud.google.com/logs/query;" +
                $"query={WebUtility.UrlEncode(query)};timeRange=PT1H;summaryFields=:true:32:beginning?" +
                $"project={projectId}");
        }

        public void OpenLogs(IProjectModelNode node)
        {
            if (node is IProjectModelInstanceNode vmNode)
            {
                OpenLogs(
                    vmNode.Instance.ProjectId,
                    "resource.type=\"gce_instance\"\n" +
                        $"resource.labels.instance_id=\"{vmNode.InstanceId}\"");
            }
            else if (node is IProjectModelZoneNode zoneNode)
            {
                OpenLogs(
                    zoneNode.Zone.ProjectId,
                    "resource.type=\"gce_instance\"\n" +
                        $"resource.labels.zone=\"{zoneNode.Zone.Name}\"");
            }
            else if (node is IProjectModelProjectNode projectNode)
            {
                OpenLogs(
                    projectNode.Project.ProjectId,
                    "resource.type=\"gce_instance\"");
            }
        }

        public void OpenVmInstanceLogDetails(string projectId, string insertId, DateTime timestamp)
        {
            OpenLogs(
                projectId,
                "resource.type=\"gce_instance\"\n" +
                    $"insertId=\"{insertId}\"\n" +
                    $"timestamp<=\"{timestamp:o}\"");
        }

        public void ConfigureIapAccess(string projectId)
        {
            Browser.Default.Navigate(
                $"https://console.cloud.google.com/security/iap?tab=ssh-tcp-resources&project={projectId}");
        }

        public void OpenMyAccount()
        {
            Browser.Default.Navigate(
                "https://myaccount.google.com/security");
        }
    }
}
