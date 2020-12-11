using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    internal static class InstanceUtil
    {
        public static async Task<string> PublicIpAddressForInstanceAsync(InstanceLocator instanceLocator)
        {
            using (var service = TestProject.CreateComputeService())
            {
                var instance = await service
                    .Instances.Get(
                            instanceLocator.ProjectId,
                            instanceLocator.Zone,
                            instanceLocator.Name)
                    .ExecuteAsync();
                return instance
                    .NetworkInterfaces
                    .EnsureNotNull()
                    .Where(nic => nic.AccessConfigs != null)
                    .SelectMany(nic => nic.AccessConfigs)
                    .EnsureNotNull()
                    .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                    .Select(accessConfig => accessConfig.NatIP)
                    .FirstOrDefault();
            }
        }
    }
}
