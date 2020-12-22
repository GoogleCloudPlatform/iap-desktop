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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using System.Linq;
using System.Net;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public static class InstanceExtensions
    {
        public static ZoneLocator GetZoneLocator(this Instance instance)
        {
            return ZoneLocator.FromString(instance.Zone);
        }

        public static InstanceLocator GetInstanceLocator(this Instance instance)
        {
            var zone = instance.GetZoneLocator();
            return new InstanceLocator(
                zone.ProjectId,
                zone.Name,
                instance.Name);
        }

        public static IPAddress PublicAddress(this Instance instance)
        {
            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Where(nic => nic.AccessConfigs != null)
                .SelectMany(nic => nic.AccessConfigs)
                .EnsureNotNull()
                .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                .Select(accessConfig => accessConfig.NatIP)
                .Where(ip => ip != null)
                .Select(ip => IPAddress.Parse(ip))
                .FirstOrDefault();
        }

        public static IPAddress InternalAddress(this Instance instance)
        {
            return instance
                .NetworkInterfaces
                .EnsureNotNull()
                .Select(nic => nic.NetworkIP)
                .Where(ip => ip != null)
                .Select(ip => IPAddress.Parse(ip))
                .FirstOrDefault();
        }
    }
}
