using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.Management
{
    public interface IInstanceControlService : IDisposable
    {
        /// <summary>
        /// Start, stop, or otherwise control the lifecycle of an instance
        /// and notify other services.
        /// </summary>
        Task ControlInstanceAsync(
            InstanceLocator instance,
            InstanceControlCommand command,
            CancellationToken cancellationToken);
    }

    public sealed class InstanceControlService : IInstanceControlService
    {
        private readonly IComputeEngineAdapter computeEngineAdapter;
        private readonly IEventService eventService;

        public InstanceControlService(
            IComputeEngineAdapter computeEngineAdapter, 
            IEventService eventService)
        {
            this.computeEngineAdapter = computeEngineAdapter;
            this.eventService = eventService;
        }

        //---------------------------------------------------------------------
        // InstanceControlService.
        //---------------------------------------------------------------------

        public async Task ControlInstanceAsync(
            InstanceLocator instance,
            InstanceControlCommand command,
            CancellationToken cancellationToken)
        {
            await this.computeEngineAdapter.ControlInstanceAsync(
                    instance,
                    command,
                    cancellationToken)
                .ConfigureAwait(false);

            await this.eventService.FireAsync(
                new InstanceRunningStateChangedEvent(
                    instance, 
                    command == InstanceControlCommand.Start ||
                        command == InstanceControlCommand.Resume))
                .ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // InstanceControlService.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            this.computeEngineAdapter.Dispose();
        }
    }


    public class InstanceRunningStateChangedEvent
    {
        public InstanceLocator Instance { get; }

        public bool IsRunning { get; }

        public InstanceRunningStateChangedEvent(
            InstanceLocator instance,
            bool isRunning)
        {
            this.Instance = instance;
            this.IsRunning = isRunning;
        }
    }
}
