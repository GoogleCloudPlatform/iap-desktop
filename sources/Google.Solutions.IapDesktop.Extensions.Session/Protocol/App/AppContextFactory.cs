using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.IapDesktop.Extensions.Session.Settings;
using Google.Solutions.Platform.Dispatch;
using System;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Session.Protocol.App
{
    internal class AppContextFactory : IProtocolContextFactory //TODO: Add test
    {
        private readonly IConnectionSettingsService settingsService;
        private readonly IIapTransportFactory transportFactory;
        private readonly IWin32ProcessFactory processFactory;

        internal AppContextFactory(
            AppProtocol protocol,
            IIapTransportFactory transportFactory,
            IWin32ProcessFactory processFactory,
            IConnectionSettingsService settingsService)
        {
            this.Protocol = protocol.ExpectNotNull(nameof(protocol));
            this.transportFactory = transportFactory.ExpectNotNull(nameof(transportFactory));
            this.processFactory = processFactory.ExpectNotNull(nameof(processFactory));
            this.settingsService = settingsService.ExpectNotNull(nameof(settingsService));

            protocol.Client.ExpectNotNull(nameof(protocol.Client));
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Protocol that this factory applies to.
        /// </summary>
        public AppProtocol Protocol { get; }

        //---------------------------------------------------------------------
        // IProtocolFactory.
        //---------------------------------------------------------------------

        public Task<IProtocolContext> CreateContextAsync(
            IProtocolTarget target,
            uint flags,
            CancellationToken cancellationToken)
        {
            target.ExpectNotNull(nameof(target));

            if (this.Protocol.IsAvailable(target) &&
                target is IProjectModelInstanceNode instance)
            {
                var context = new AppProtocolContext(
                    this.Protocol,
                    this.transportFactory,
                    this.processFactory,
                    instance.Instance);

                var contextFlags = (AppProtocolContextFlags)flags;
                if (contextFlags.HasFlag(AppProtocolContextFlags.TryUseRdpNetworkCredentials))
                {
                    //
                    // See if we have RDP credentials.
                    //
                    var settings = this.settingsService
                        .GetConnectionSettings(instance)
                        .TypedCollection;

                    if (!string.IsNullOrEmpty(settings.RdpUsername.StringValue))
                    {
                        context.NetworkCredential = new NetworkCredential(
                            settings.RdpUsername.StringValue,
                            (SecureString)settings.RdpPassword.Value,
                            settings.RdpDomain.StringValue);
                    }
                }
                else if (contextFlags != AppProtocolContextFlags.None)
                {
                    throw new ArgumentException("Unsupported flags: " + contextFlags);
                }

                return Task.FromResult<IProtocolContext>(context);
            }
            else
            {
                throw new ProtocolTargetException(
                    $"The protocol '{this.Protocol.Name}' can't be used for {target}",
                    HelpTopics.AppProtocols);
            }
        }

        public bool TryParse(Uri uri, out ProtocolTargetLocator locator)//TODO: Add test
        {
            locator = null;
            return false;
        }
    }

    [Flags]
    public enum AppProtocolContextFlags : uint
    {
        None,

        /// <summary>
        /// Use RDP credentials as network credentials.
        /// </summary>
        TryUseRdpNetworkCredentials = 1,
    }
}
