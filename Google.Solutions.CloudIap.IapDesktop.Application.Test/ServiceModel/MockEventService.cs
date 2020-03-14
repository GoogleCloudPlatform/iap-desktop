using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Test.ServiceModel
{
    class MockEventService : IEventService
    {
        public virtual void Bind<TEvent>(Action<TEvent> handler)
        {
        }

        public virtual void Bind<TEvent>(Func<TEvent, Task> handler)
        {
        }

        public Task FireAsync<TEvent>(TEvent eventObject)
        {
            return Task.FromResult(0);
        }
    }
}
