using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Logging.v2;
using Google.Apis.Services;
using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Extensions;
using Google.Solutions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    Console.WriteLine($"    > {placement.From} - {placement.To} on {placement.ServerId}");
                }
            }
        }

        static void Main(string[] args)
        {
            AnalyzeAsync(args[0], 40).Wait();
        }
    }
}
