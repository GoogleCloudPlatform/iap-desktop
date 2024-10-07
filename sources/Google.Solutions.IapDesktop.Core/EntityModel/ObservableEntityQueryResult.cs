using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Threading;
using Google.Solutions.IapDesktop.Core.EntityModel.Query;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Result of an entity query that is kept up to date when
    /// the underlying data changes.
    /// </summary>
    public sealed class ObservableEntityQueryResult<TEntity>
        : ObservableCollection<EntityQueryResultItem<TEntity>> // TODO: test
        where TEntity : IEntity<ILocator>
    {
        private readonly ISubscription propertyChanged;
        private readonly ISubscription deleted;

        public ObservableEntityQueryResult(
            IList<EntityQueryResultItem<TEntity>> list,
            IEventQueue eventQueue,
            SynchronizationContext synchronizationContext) : base(list)
        {
            //
            // NB. Events are rare and results tend to be small, so
            // linear searches are fine.
            //

            // 
            // NB. We must ensure that all events are raised on the
            // original synchonization context (which is likely
            // to be a UI context).
            //

            this.propertyChanged = eventQueue.Subscribe<EntityPropertyChangedEvent>(
                e => {
                    if (this.FirstOrDefault(i => i.Entity.Locator == e.Locator)
                        is IObservableAspect item)
                    {
                        //
                        // Notify aspect.
                        //
                        synchronizationContext.Post(
                            () => item.OnEntityChanged(e));
                    }
                });
            this.deleted = eventQueue.Subscribe<EntityDeletedEvent>(
                e => {
                    //
                    // Remove the corresponding item from this
                    // collection, let base class raise event.
                    //
                    if (this.FirstOrDefault(i => i.Entity.Locator == e.Locator)
                        is EntityQueryResultItem<TEntity> item)
                    {
                        synchronizationContext.Post(
                            () => this.Remove(item));
                    }
                });
        }

        //TODO: Subscribe in context and use weak references instead.

        public void Dispose()
        {
            //
            // Stop listening to events.
            //
            this.propertyChanged.Dispose();
            this.deleted.Dispose();
        }
    }

    public interface IObservableAspect : INotifyPropertyChanged
    {
        /// <summary>
        /// Invoked when the aspect is part of an observable
        /// result and the underlying entity changed.
        /// </summary>
        /// <remarks>
        /// Implementing classes should raise a PropertyChanged
        /// event.
        /// </remarks>
        void OnEntityChanged(EntityPropertyChangedEvent ev);
    }
}
