using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.ClientApp
{
    internal interface IWindowsProtocolClient : IAppProtocolClient
    {
        NetworkCredentialType RequiredCredential { get; }
    }

    public enum NetworkCredentialType
    {
        /// <summary>
        /// Use default network credentials.
        /// </summary>
        Default,

        /// <summary>
        /// Use RDP credentials as network credentials,
        /// if available.
        /// </summary>
        Rdp,

        /// <summary>
        /// Prompt user for network credentials.
        /// </summary>
        Prompt
    }
}
