using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Services;
using Google.Solutions.Compute.Test.Env;
using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.Events.Lifecycle;
using Google.Solutions.LogAnalysis.Extensions;
using Google.Solutions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Test.Extensions
{
    [TestFixture]
    [Category("IntegrationTest")]
    public class TestListLogEntriesExtensions
    {
        [Test]
        public async Task WhenInstanceCreated_ThenListLogEntriesReturnsInsertEvent(
            [LinuxInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();
            var instanceRef = await testInstance.GetInstanceAsync();

            var loggingService = new LoggingService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });


            var request = new ListLogEntriesRequest()
            {
                ResourceNames = new[]
                {
                    "projects/" + Defaults.ProjectId
                },
                Filter = $"resource.type=\"gce_instance\" "+
                    $"AND protoPayload.methodName:* "+
                    $"AND timestamp > {DateTime.Now.AddDays(-1):yyyy-MM-dd}",
                PageSize = 1000,
                OrderBy = "timestamp desc"
            };

            var events = new List<EventBase>();
            var instanceBuilder = new InstanceSetHistoryBuilder();
            await loggingService.Entries.ListEventsAsync(
                request,
                events.Add,
                new Apis.Util.ExponentialBackOff());

            var insertEvent = events.OfType<InsertInstanceEvent>().First();
            Assert.AreEqual(insertEvent.InstanceReference, instanceRef);
        }
    }
}
