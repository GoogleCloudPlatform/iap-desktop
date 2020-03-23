using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.ServiceModel
{
    class MockEventService : IEventService
    {
        public virtual void BindHandler<TEvent>(Action<TEvent> handler)
        {
        }

        public virtual void BindAsyncHandler<TEvent>(Func<TEvent, Task> handler)
        {
        }

        public Task FireAsync<TEvent>(TEvent eventObject)
        {
            return Task.FromResult(0);
        }
    }
}
