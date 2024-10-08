using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Google.Solutions.IapDesktop.Core.EntityModel.Query
{
    /// <summary>
    /// Result of an entity query that is kept up to date when
    /// the underlying data changes.
    /// </summary>
    /// <remarks>
    /// The class is not thread-safe and must only be used on the
    /// GUI thread (i.e., the thread on which the EventQueue
    /// delivers events).
    /// </remarks>
    public sealed class ObservableEntityQueryResult<TEntity>
        : ObservableCollection<EntityQueryResultItem<TEntity>> // TODO: test
        where TEntity : IEntity<ILocator>
    {
        private readonly ISubscription propertyChanged;
        private readonly ISubscription deleted;

#if DEBUG
        private readonly int threadId = Environment.CurrentManagedThreadId;
#endif

        [Conditional("DEBUG")]
        private void CheckIsRunningOnRightThread() 
        { 
            Debug.Assert(
                this.threadId == Environment.CurrentManagedThreadId,
                "Result must not be accessed from an arbitrary thread context");
        }

        public ObservableEntityQueryResult(
            IList<EntityQueryResultItem<TEntity>> list,
            IEventQueue eventQueue) : base(list)
        {
            //
            // Subscribe to entity events using a weak subscription,
            // and "demultiplex" any events we receive.
            //

            //
            // NB. Events are rare and results tend to be small, so
            // linear searches are fine.
            //

            this.propertyChanged = eventQueue.Subscribe<EntityPropertyChangedEvent>(
                e => {
                    CheckIsRunningOnRightThread();
                    this
                        .FirstOrDefault(i => i.Entity.Locator.Equals(e.Locator))?
                        .Notify(e);
                },
                SubscriptionOptions.WeakSubscriberReference);
            this.deleted = eventQueue.Subscribe<EntityRemovedEvent>(
                e => {
                    // 
                    // NB. The EventQueue delivers callbacks on a designated
                    // thread which should be the same as the caller's thread.
                    // So we don't need to worry about concurrent modifications
                    // or locking here.
                    //
                    CheckIsRunningOnRightThread();

                    //
                    // Remove the corresponding item from this
                    // collection, let base class raise event.
                    //
                    if (this.FirstOrDefault(i => i.Entity.Locator.Equals(e.Locator))
                        is EntityQueryResultItem<TEntity> item)
                    {
                        this.Remove(item);
                    }
                },
                SubscriptionOptions.WeakSubscriberReference);
        }

        public new IEnumerator<EntityQueryResultItem<TEntity>> GetEnumerator()
        {
            CheckIsRunningOnRightThread();
            return base.GetEnumerator();
        }

        public void Dispose()
        {
            //
            // Stop listening to events.
            //
            this.propertyChanged.Dispose();
            this.deleted.Dispose();
        }
    }
}