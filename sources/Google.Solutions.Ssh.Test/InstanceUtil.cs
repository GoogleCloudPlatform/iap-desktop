using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.ApiExtensions.Instance;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test
{
    internal static class InstanceUtil
    {
        public static async Task<IPAddress> PublicIpAddressForInstanceAsync(
            InstanceLocator instanceLocator)
        {
            using (var service = TestProject.CreateComputeService())
            {
                var instance = await service
                    .Instances.Get(
                            instanceLocator.ProjectId,
                            instanceLocator.Zone,
                            instanceLocator.Name)
                    .ExecuteAsync();
                var ip = instance
                    .NetworkInterfaces
                    .EnsureNotNull()
                    .Where(nic => nic.AccessConfigs != null)
                    .SelectMany(nic => nic.AccessConfigs)
                    .EnsureNotNull()
                    .Where(accessConfig => accessConfig.Type == "ONE_TO_ONE_NAT")
                    .Select(accessConfig => accessConfig.NatIP)
                    .FirstOrDefault();
                return IPAddress.Parse(ip);
            }
        }

        public static Task AddPublicKeyToMetadata(
            InstanceLocator instanceLocator,
            string username,
            RSACng key)
            => AddPublicKeyToMetadata(
                instanceLocator,
                username,
                Convert.ToBase64String(key.ToSshPublicKey()));

        public static async Task AddPublicKeyToMetadata(
            InstanceLocator instanceLocator,
            string username,
            string rsaPublicKey)
        {
            using (var service = TestProject.CreateComputeService())
            {
                await service.Instances.AddMetadataAsync(
                    instanceLocator,
                    new Metadata()
                    {
                        Items = new[]
                        {
                            new Metadata.ItemsData()
                            {
                                Key = username,
                                Value = $"ssh-rsa {rsaPublicKey} {username}"
                            }
                        }
                    },
                    CancellationToken.None);
            }
        }
    }
}
