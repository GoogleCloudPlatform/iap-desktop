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


            var instanceSetBuilder = new InstanceSetHistoryBuilder(
                DateTime.UtcNow.AddDays(-days),
                DateTime.UtcNow);
            await instanceSetBuilder.AddExistingInstances(
                computeService.Instances,
                computeService.Disks,
                projectId);

            await loggingService.Entries.ListInstanceEventsAsync(
                new[] { projectId },
                DateTime.Now.AddDays(-days),
                instanceSetBuilder);

            var set = instanceSetBuilder.Build();

            Console.WriteLine(
                "== Instances ==================================================================");
            foreach (var instance in set.Instances)
            {
                Console.WriteLine($"  Instance     {instance.Reference} ({instance.InstanceId})");
                Console.WriteLine($"     State:    {instance.State}");
                Console.WriteLine($"     Image:    {instance.Image}");
                Console.WriteLine($"     Tenancy:  {instance.Tenancy}");
                Console.WriteLine($"     Placements:");

                foreach (var placement in instance.Placements.OrderBy(p => p.From))
                {
                    Console.WriteLine($"      - {placement}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(
                "== Nodes ======================================================================");

            var nodeSet = NodeSetHistory.FromInstancyHistory(set.Instances);
            foreach (var node in nodeSet.Nodes)
            {
                Console.WriteLine($"  Node          {node.ServerId}");
                Console.WriteLine($"     First use: {node.FirstUse}");
                Console.WriteLine($"     To:        {node.LastUse}");
                Console.WriteLine($"     Peak VMs:  {node.PeakConcurrentPlacements}");
                Console.WriteLine($"     Placements:");

                foreach (var placement in node.Placements.OrderBy(p => p.From))
                {
                    Console.WriteLine($"      - {placement}");
                }


                Console.WriteLine($"     Statistics:");
                foreach (var dp in node.PlacementHistogram)
                {
                    Console.WriteLine($"      - {dp.Timestamp} {new string('#', dp.Value)}");
                }
            }
        }

        static void Main(string[] args)
        {
            AnalyzeAsync(args[0], 40).Wait();
        }
    }
}
