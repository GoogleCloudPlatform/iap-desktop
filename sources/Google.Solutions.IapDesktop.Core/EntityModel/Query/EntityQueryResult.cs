using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel.Query
{
    /// <summary>
    /// Result of an entity query.
    /// </summary>
    public sealed class EntityQueryResult<TEntity>  // TODO: test
        : ReadOnlyCollection<EntityQueryResult<TEntity>.Item>, IDisposable
        where TEntity : IEntity<ILocator>
    {
        public EntityQueryResult(IList<Item> list) : base(list)
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// An entity with associated aspects.
        /// </summary>
        public class Item
        {
            private readonly Dictionary<Type, object?> aspects;
            private readonly Dictionary<Type, DeriveAspectDelegate> derivedAspects;

            internal static async Task<Item> CreateAsync(
                TEntity entity,
                Dictionary<Type, Task<object?>> aspectTasks,
                Dictionary<Type, DeriveAspectDelegate> derivedAspects)
            {
                await Task
                    .WhenAll(aspectTasks.Values)
                    .ConfigureAwait(false);

                return new Item(
                    entity,
                    aspectTasks.ToDictionary(
                        item => item.Key,
                        item => item.Value.Result),
                    derivedAspects);
            }

            private Item(
                TEntity entity,
                Dictionary<Type, object?> aspectValues,
                Dictionary<Type, DeriveAspectDelegate> derivedAspects)
            {
                this.Entity = entity;
                this.aspects = aspectValues;
                this.derivedAspects = derivedAspects;
            }

            /// <summary>
            /// The entity itself.
            /// </summary>
            public TEntity Entity { get; }

            /// <summary>
            /// Get aspect for this entity.
            /// </summary>
            public TAspect? Aspect<TAspect>() where TAspect : class
            {
                if (this.aspects.ContainsKey(typeof(TAspect)))
                {
                    return this.aspects[typeof(TAspect)] as TAspect;
                }
                else if (this.derivedAspects.ContainsKey(typeof(TAspect)))
                {
                    return this.derivedAspects[typeof(TAspect)](this.aspects) as TAspect;
                }
                else
                {
                    throw new ArgumentException(
                        $"The query does not include aspect '${typeof(TAspect)}'");
                }
            }
        }
    }
}
