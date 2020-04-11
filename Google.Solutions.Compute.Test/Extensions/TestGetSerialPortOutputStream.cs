
using Google.Solutions.Compute.Test.Env;
using Google.Solutions.Compute.Extensions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Google.Apis.Compute.v1;

namespace Google.Solutions.Compute.Test.Extensions
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("Windows")]
    public class TestGetSerialPortOutputStream
    {
        private InstancesResource instancesResource;

        [SetUp]
        public void SetUp()
        {
            this.instancesResource = ComputeEngine.Connect().Service.Instances;
        }

        [Test]
        public async Task WhenLaunchingInstance_ThenInstanceSetupFinishedTextAppearsInStream(
           [WindowsInstance] InstanceRequest testInstance)
        {
            await testInstance.AwaitReady();

            var stream = this.instancesResource.GetSerialPortOutputStream(
                testInstance.InstanceReference, 
                1);

            var startTime = DateTime.Now;
            
            while (DateTime.Now < startTime.AddMinutes(3))
            {
                var log = await stream.ReadAsync();
                if (log.Contains("Instance setup finished"))
                {
                    return;
                }
            }

            Assert.Fail("Timeout waiting for serial console output to appear");
        }

    }
}
