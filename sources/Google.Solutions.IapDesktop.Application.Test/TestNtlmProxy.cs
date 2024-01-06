using Google.Solutions.Apis.Client;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Client;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestNtlmProxy
    {
        private static readonly Uri proxyAddress = null;
        private static readonly NetworkCredential proxyCredential =  null;

        [SetUp]
        public void Setup()
        {
            if (proxyAddress == null || proxyCredential == null)
            {
                Assert.Inconclusive("Proxy credentials not set");
            }

            var proxy = new HttpProxyAdapter();
            proxy.ActivateCustomProxySettings(
                proxyAddress,
                Enumerable.Empty<string>(),
                proxyCredential);
        }

        [Test]
        public async Task SequentialRequests()
        {
            var compute = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(ServiceRoute.Public),
                TestProject.AdminAuthorization,
                TestProject.UserAgent);

            for (int i = 0; i < 10; i++)
            {
                await compute
                    .ListInstancesAsync(
                        new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        public static ComputeEngineClient _client;

        [Test]
        public async Task ParallelRequests()
        {
            var compute = new ComputeEngineClient(
                ComputeEngineClient.CreateEndpoint(ServiceRoute.Public),
                TestProject.AdminAuthorization,
                TestProject.UserAgent);
            _client = compute;
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(compute
                    .ListInstancesAsync(
                        new ZoneLocator(TestProject.ProjectId, "us-central1-a"),
                        CancellationToken.None));
            }

            await Task.WhenAll(tasks);
        }
    }
}
