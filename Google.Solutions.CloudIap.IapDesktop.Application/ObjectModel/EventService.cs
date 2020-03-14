using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    public interface IEventService
    {
        void Bind<TEvent>(Func<TEvent, Task> handler);
        void Bind<TEvent>(Action<TEvent> handler);


        /// <summary>
        /// Invoke all event handlers registered for the given event type.
        /// </summary>
        Task FireAsync<TEvent>(TEvent eventObject);
    }

    /// <summary>
    /// Allows events to be delivered to handlers in a multi-cast manner.
    /// 
    /// Invoker thread: Any
    /// Execution Thread: UI (ensured by EventService)
    /// Continuation thread: Same as invoker (ensured by EventService)
    /// </summary>
    public class EventService : IEventService
    {
        private static readonly Task CompletedTask = Task.FromResult(0);

        private readonly ISynchronizeInvoke invoker;
        private readonly IDictionary<Type, List<Func<object, Task>>> bindings = new Dictionary<Type, List<Func<object, Task>>>();

        public EventService(ISynchronizeInvoke invoker)
        {
            this.invoker = invoker;
        }

        public void Bind<TEvent>(Func<TEvent, Task> handler)
        {
            if (!this.bindings.ContainsKey(typeof(TEvent)))
            {
                this.bindings.Add(typeof(TEvent), new List<Func<object, Task>>());
            }

            this.bindings[typeof(TEvent)].Add(e => handler((TEvent)e));
        }

        public void Bind<TEvent>(Action<TEvent> handler)
        {
            Bind<TEvent>(e =>
            {
                handler(e);
                return CompletedTask;
            });
        }

        private async Task FireOnUiThread<TEvent>(TEvent eventObject)
        {
            Debug.Assert(!this.invoker.InvokeRequired, "FireOnUiThread must be called on UI thread");

            if (this.bindings.TryGetValue(typeof(TEvent), out var handlers))
            {
                // Run handlers.
                foreach (var handler in handlers)
                {
                    // Invoke handler, but make sure we return to the UI thread.
                    await handler(eventObject).ConfigureAwait(continueOnCapturedContext: true);
                }
            }
        }


        private void FireOnUiThread<TEvent>(TEvent eventObject, TaskCompletionSource<TEvent> completionSource)
        {
            var fireTask = FireOnUiThread(eventObject);
            fireTask.ContinueWith(t =>
                {
                    try
                    {
                        t.Wait();
                        completionSource.SetResult(eventObject);
                    }
                    catch (Exception e)
                    {
                        completionSource.SetException(e);
                    }
                });
        }


        /// <summary>
        /// Invoke all event handlers registered for the given event type.
        /// </summary>
        public Task FireAsync<TEvent>(TEvent eventObject)
        {
            if (this.invoker.InvokeRequired)
            {
                // We are running on some non-UI thread. Switch to UI
                // thread to handle events.

                var completionSource = new TaskCompletionSource<TEvent>();

                this.invoker.Invoke((Action)(() => FireOnUiThread<TEvent>(eventObject, completionSource)), null);

                return completionSource.Task;
            }
            else
            {
                // We are on the UI thread, so we can simply dispatch the call
                // directly.
                return FireOnUiThread(eventObject);
            }
        }
    }
}
