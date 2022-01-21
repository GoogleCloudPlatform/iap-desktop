using Google.Solutions.Common.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Iap
{
    /// <summary>
    /// Locator for an IAP a generic ("on-prem") network endpoint.
    /// </summary>
    internal class NetworkEndpointLocator : ResourceLocator, IEquatable<NetworkEndpointLocator>
    {
        /// <summary>
        /// Region of of the network resource or VPN endpoint.
        /// </summary>
        public string Region { get; }

        /// <summary>
        /// Name of VPC network.
        /// </summary>
        public string Network { get; }

        public override string ResourceType => "images";

        public NetworkEndpointLocator(
            string projectId,
            string region,
            string network,
            string name)
            : base(projectId, name)
        {
            this.Region = region;
            this.Network = network;
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return
                this.ProjectId.GetHashCode() ^
                this.Region.GetHashCode() ^
                this.Network.GetHashCode() ^
                this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Name} in projects/${this.ProjectId}/global/networks/{this.Network}";
        }

        public bool Equals(NetworkEndpointLocator other)
        {
            return other is object &&
                this.Name == other.Name &&
                this.Region == other.Region &&
                this.Network == other.Network &&
                this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkEndpointLocator locator && Equals(locator);
        }

        public static bool operator ==(NetworkEndpointLocator obj1, NetworkEndpointLocator obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(NetworkEndpointLocator obj1, NetworkEndpointLocator obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
