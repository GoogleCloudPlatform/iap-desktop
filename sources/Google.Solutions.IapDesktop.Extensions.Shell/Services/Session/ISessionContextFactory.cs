using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using MSTSCLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection.TransportParameters;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Session
{
    internal interface ISessionContextFactory
    {
        ISessionContext<SshCredential> CreateSshSession(IProjectModelNode node);
        
        ISessionContext<RdpCredential> CreateRdpSession(IProjectModelNode node);

        ISessionContext<RdpCredential> CreateRdpSession(IapRdpUrl url);
    }




    internal interface ISessionContext<TCredential>
        where TCredential : ICredential
    {
        /// <summary>
        /// Target instance of this session.
        /// </summary>
        InstanceLocator Instance { get; }

        /// <summary>
        /// Create or negotiate a credential. 
        /// This should be performed in a job.
        /// </summary>
        Task<TCredential> CreateCredentialAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Create a transport, which might involve creating a tunnel.
        /// This should be performed in a job.
        /// </summary>
        Task<ITransport> CreateTransportAsync(CancellationToken cancellationToken);
    }

    internal class RdpSessionContext : ISessionContext<RdpCredential>
    {
        bool AllowPersistentCredentials { get; set; }

        // all settings
    }


    internal class SshSessionContext : ISessionContext<SshCredential>
    {
        // all settings
    }




    internal interface ITransport
    {
        /// <summary>
        /// Connection target.
        /// </summary>
        InstanceLocator Instance { get; }

        /// <summary>
        /// Type of transport.
        /// </summary>
        TransportType Type { get; }

        /// <summary>
        /// Endpoint to connect to. This might be a localhost endpoint.
        /// </summary>
        System.Net.IPEndPoint Endpoint { get; }
    }




    internal interface ICredential
    {
        /// <summary>
        /// A description of the credential that is safe to display.
        /// </summary>
        string ToString();
    }

    // NOW:

    // RDP: Use existing RdpCredential
    // SSH: Use existing AuthorizedKeyPair

    // LATER:

    internal class RdpCredential : ICredential
    {
        //void Apply(IMsRdpClientAdvancedSettings6 client);
    }

    internal class SshCredential : ICredential
    {
        // AuthMethod : { pwd, pubkey
        // Sign( ... )
        // Prompt( ... )
    }

    internal class OsLoginAuthorizedKey : SshCredential
    {

    }

    internal class MetadataAuthorizedKey : SshCredential
    {

    }
}
