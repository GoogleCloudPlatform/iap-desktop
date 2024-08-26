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

using Google.Solutions.Apis.Auth;
using Google.Solutions.Apis.Auth.Gaia;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Platform.Net;
using System;
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Client
{
    public interface ICloudConsoleClient
    {
        /// <summary>
        /// Open IAP-TCP security page.
        /// </summary>
        void OpenIapSecurity(string projectId);

        /// <summary>
        /// Open instance details page.
        /// </summary>
        void OpenInstanceDetails(InstanceLocator instance);

        /// <summary>
        /// Open instance overview page.
        /// </summary>
        void OpenInstanceList(ProjectLocator project);

        /// <summary>
        /// Open instance overview page.
        /// </summary>
        void OpenInstanceList(ZoneLocator zone);

        /// <summary>
        /// Open Logs viewer.
        /// </summary>
        void OpenLogs(IProjectModelNode node);

        /// <summary>
        /// Open log entry.
        /// </summary>
        void OpenVmInstanceLogDetails(string projectId, string insertId, DateTime? timestamp);
    }

    public class CloudConsoleClient : ICloudConsoleClient
    {
        private readonly IAuthorization authorization;
        private readonly IBrowser browser;

        public CloudConsoleClient(IAuthorization authorization, IBrowser browser)
        {
            this.authorization = authorization.ExpectNotNull(nameof(authorization));
            this.browser = browser.ExpectNotNull(nameof(browser));
        }

        private Uri BaseUri
        {
            get
            {
                if (this.authorization.Session is IGaiaOidcSession)
                {
                    if (this.authorization.DeviceEnrollment.State == DeviceEnrollmentState.Enrolled)
                    {
                        return new Uri("https://console-secure.cloud.google.com/");
                    }
                    else
                    {
                        return new Uri("https://console.cloud.google.com/");
                    }
                }
                else
                {
                    return new Uri("https://console.cloud.google/");
                }
            }
        }

        private void Open(string path)
        {
            var rawUri = new Uri(this.BaseUri, path);

            //
            // The user might not have an active browser session.
            //
            // Create a domain-specific URI to expedite the sign-in process.
            // This is primarily relevant for workforce identity users because
            // they might otherwise be prompted for their (difficult to remember)
            // provider name.
            //
            var domainSpecificUri = this.authorization
                .Session
                .CreateDomainSpecificServiceUri(rawUri);

            this.browser.Navigate(domainSpecificUri);
        }

        //---------------------------------------------------------------------
        // ICloudConsole.
        //---------------------------------------------------------------------

        public void OpenInstanceDetails(InstanceLocator instance)
        {
            Open("/compute/instancesDetail/zones/" +
                 $"{instance.Zone}/instances/{instance.Name}?project={instance.ProjectId}");
        }

        public void OpenInstanceList(ProjectLocator project)
        {
            Open($"/compute/instances?project={project.ProjectId}");
        }

        public void OpenInstanceList(ZoneLocator zone)
        {
            var query = "[{\"k\":\"zoneForFilter\",\"v\":\"" + zone.Name + "\"}]";

            Open($"/compute/instances?project={zone.ProjectId}&instancesquery={WebUtility.UrlEncode(query)}");
        }

        private void OpenLogs(string projectId, string query)
        {
            Open("/logs/query;" +
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

        public void OpenVmInstanceLogDetails(string projectId, string insertId, DateTime? timestamp)
        {
            if (timestamp  == null)
            {
                timestamp = DateTime.UtcNow;
            }

            OpenLogs(
                projectId,
                "resource.type=\"gce_instance\"\n" +
                    $"insertId=\"{insertId}\"\n" +
                    $"timestamp<=\"{timestamp:o}\"");
        }

        public void OpenIapSecurity(string projectId)
        {
            Open($"/security/iap?tab=ssh-tcp-resources&project={projectId}");
        }
    }
}
