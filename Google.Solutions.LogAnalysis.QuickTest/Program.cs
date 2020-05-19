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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.LogAnalysis.QuickTest
{
    class Program
    {
        internal static string ShortIdFromUrl(string url) => url.Substring(url.LastIndexOf("/") + 1);

        private static LoggingService CreateLoggingService()
        {
            return new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.GetApplicationDefault()
            });
        }

        private static ComputeService CreateComputeService()
        {
            return new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential.GetApplicationDefault()
            });
        }

        private static async Task DownloadAsync(string projectId, int days, string filePath)
        {
            var loggingService = CreateLoggingService();
            var computeService = CreateComputeService();

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

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                instanceSetBuilder.Build().Serialize(writer);
            }
        }

        private static void Analyze(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var set = InstanceSetHistory.Deserialize(reader);

                //Console.WriteLine(
                //    "== Instances ==================================================================");
                //foreach (var instance in set.Instances)
                //{
                //    Console.WriteLine($"  Instance     {instance.Reference} ({instance.InstanceId})");
                //    Console.WriteLine($"     State:    {instance.State}");
                //    Console.WriteLine($"     Image:    {instance.Image}");
                //    Console.WriteLine($"     Placements:");

                //    foreach (var placement in instance.Placements.OrderBy(p => p.From))
                //    {
                //        Console.WriteLine($"      - {placement}");
                //    }
                //}

                Console.WriteLine();
                Console.WriteLine(
                    "== Images =====================================================================");

                foreach (var image in set.Instances.Select(i => i.Image).Distinct())
                {
                    Console.WriteLine($"      - {image}");
                }

                Console.WriteLine();
                Console.WriteLine(
                    "== Nodes ======================================================================");

                var nodeSet = NodeSetHistory.FromInstancyHistory(set.Instances, true);
                foreach (var node in nodeSet.Nodes)
                {
                    Console.WriteLine($"  Node          {node.ServerId ?? "FLEET"}");
                    Console.WriteLine($"     First use: {node.FirstUse}");
                    Console.WriteLine($"     To:        {node.LastUse}");
                    Console.WriteLine($"     Peak VMs:  {node.PeakConcurrentPlacements}");
                    Console.WriteLine($"     Placements:");

                    foreach (var placement in node.Placements.OrderBy(p => p.From))
                    {
                        Console.WriteLine($"      - {placement}");
                    }

                    Console.WriteLine($"     Statistics:");
                    foreach (var dp in node.MaxInstancePlacementsByDay)
                    {
                        Console.WriteLine($"      - {dp.Timestamp:yyyy-MM-dd} \t|{new string('#', dp.Value)} ({dp.Value})");
                    }
                }

                Console.WriteLine();
                Console.WriteLine(
                    "== Nodes Summary ===============================================================");


                foreach (var dp in nodeSet.MaxNodesByDay)
                {
                    Console.WriteLine($"      - {dp.Timestamp:yyyy-MM-dd} \t|{new string('#', dp.Value)} ({dp.Value})");
                }
            }
        }

        private static void AnalyzeGui(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                var instanceSet = InstanceSetHistory.Deserialize(reader);
                var nodeSet = NodeSetHistory.FromInstancyHistory(instanceSet.Instances, false);
                
                Application.Run(new Report(instanceSet, nodeSet));
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 4 && args[0] == "download")
            {
                DownloadAsync(args[1], int.Parse(args[2]), args[3]).Wait();
            }
            else if (args.Length >= 2 && args[0] == "analyze")
            {
                Analyze(args[1]);
            }
            else if (args.Length >= 2 && args[0] == "analyze-gui")
            {
                AnalyzeGui(args[1]);
            }
            else
            {
                Console.WriteLine("Usage: <program> download <project> <days> <file>");
                Console.WriteLine("       <program> analyze <file>");
                Environment.Exit(1);
            }
        }
    }
}
