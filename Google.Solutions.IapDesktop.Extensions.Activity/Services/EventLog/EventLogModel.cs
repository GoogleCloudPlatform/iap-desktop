using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog
{
    internal class EventLogModel : IEventProcessor
    {
        private readonly List<EventBase> events = new List<EventBase>();
        public IEnumerable<EventBase> Events => this.events;
        public EventOrder ExpectedOrder => EventOrder.NewestFirst;
        public IEnumerable<string> SupportedSeverities { get; }
        public IEnumerable<string> SupportedMethods { get; }

        public EventLogModel(
            IEnumerable<string> severities,
            IEnumerable<string> methods)
        {
            this.SupportedSeverities = severities;
            this.SupportedMethods = methods;
        }

        public void Process(EventBase e)
        {
            this.events.Add(e);
        }
    }
}
