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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Logging.v2;
using Google.Apis.Services;
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Extensions;
using Google.Solutions.LogAnalysis.History;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.QuickTest
{
    class Program
    {
        internal static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private static async Task AnalyzeAsync(string projectId, int days)
        {
            var loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.GetApplicationDefault()
            });

            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.GetApplicationDefault()
            });


            var instanceSetBuilder = new InstanceSetHistoryBuilder();

            var instances = await computeService.Instances.AggregatedList(projectId).ExecuteAsync();
            foreach (var list in instances.Items.Values)
            {
                if (list.Instances == null)
                {
                    continue;
                }

                foreach (var instance in list.Instances)
                {
                    instanceSetBuilder.AddExistingInstance(
                        (long)instance.Id.Value,
                        new VmInstanceReference(
                            projectId,
                            ShortIdFromUrl(instance.Zone),
                            instance.Name),
                        instance.Disks
                            .EnsureNotNull()
                            .Where(d => d.Boot != null && d.Boot.Value)
                            .EnsureNotNull()
                            .Where(d => d.InitializeParams != null)
                            .Select(d => GlobalResourceReference.FromString(d.InitializeParams.SourceImage))
                            .FirstOrDefault(),
                        instance.Status == "RUNNING"
                            ? InstanceState.Running
                            : InstanceState.Terminated,
                        DateTime.Now,
                        instance.Scheduling.NodeAffinities != null && instance.Scheduling.NodeAffinities.Any()
                            ? Tenancy.SoleTenant
                            : Tenancy.Fleet);
                }
            }

            await loggingService.Entries.ListInstanceEventsAsync(
                new[] { projectId },
                DateTime.Now.AddDays(-days),
                instanceSetBuilder);

            var set = instanceSetBuilder.Build();

            Console.WriteLine($"Instances with incomplete info: {set.InstancesWithIncompleteInformation.Count()}");
            Console.WriteLine($"Instances with complete info: {set.Instances.Count()}");

            foreach (var instance in set.Instances)
            {
                Console.WriteLine($"  Instance {instance.Reference} ({instance.InstanceId}) of {instance.Image}");
                foreach (var placement in instance.Placements)
                {
                    Console.WriteLine($"    > {placement}");
                }
            }
        }

        static void Main(string[] args)
        {
            AnalyzeAsync(args[0], 40).Wait();
        }
    }
}
