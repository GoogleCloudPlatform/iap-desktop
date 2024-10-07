using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    internal sealed class EventRouter<TEvent> : IDisposable
    {
        private readonly ISubscription subscription;

        private readonly object listenerLock = new object();
        private List<WeakReference<IEventListener<TEvent>>> listeners 
            = new List<WeakReference<IEventListener<TEvent>>>();

        public EventRouter(IEventQueue eventQueue)
        {
            this.subscription = eventQueue.Subscribe<TEvent>(RouteEvent);
        }

        private void RouteEvent(TEvent ev)
        { 
        }

        public void AddListener(IEventListener<TEvent> listener)
        {
            lock (this.listenerLock)
            {
                if (this.listeners.Count > 100)
                {
                    this.listeners = this.listeners
                        .Where(r => r.TryGetTarget(out var _))
                        .ToList();
                }

                this.listeners.Add(new WeakReference<IEventListener<TEvent>>(listener));
            }
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
    }

    internal interface IEventListener<TEvent> 
    {
        void OnEvent(TEvent ev);
    }
}
