using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    /// <summary>
    /// Delegate for deriving an aspect from one or more input aspects.
    /// </summary>
    internal delegate object? DeriveAspectDelegate(IDictionary<Type, object?> aspectValues);

    /// <summary>
    /// Delegate for looking up entities.
    /// </summary>
    internal delegate Task<ICollection<TEntity>> QueryDelegate<TEntity>(CancellationToken cancellationToken)
        where TEntity : IEntity<ILocator>;

    public static class EntityContextQueryExtensions // TODO: test
    {
        /// <summary>
        /// Query entities of a certain type, or subtype thereof.
        /// </summary>
        public static EntityQueryBuilder<TEntity> Entities<TEntity>(
            this EntityContext context)
            where TEntity : IEntity<ILocator>
        {
            return new EntityQueryBuilder<TEntity>(context);
        }
    }

    public class EntityQueryBuilder<TEntity>
        where TEntity : IEntity<ILocator>
    {
        private readonly EntityContext context;

        internal EntityQueryBuilder(EntityContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Query entities by performing a search.
        /// </summary>
        public Query Search<TQuery>(TQuery query)
        {
            return new Query(
                this.context,
                ct => this.context.SearchAsync<TQuery, TEntity>(query, ct));
        }

        /// <summary>
        /// Query entities by performing a wildcard search.
        /// </summary>
        public Query List()
        {
            return Search(AnyQuery.Instance);
        }

        /// <summary>
        /// Query entities by parent locator.
        /// </summary>
        /// <param name="locator">Locator of parent entity</param>
        public Query ByAncestor(ILocator locator)
        {
            return new Query(
                this.context,
                ct => this.context.ExpandAsync<TEntity>(locator, ct));
        }

        public class Query
        {
            private readonly EntityContext context;
            private readonly QueryDelegate<TEntity> query;
            private readonly Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>> aspects
                = new Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>>();
            private readonly Dictionary<Type, DeriveAspectDelegate> derivedAspects
                = new Dictionary<Type, DeriveAspectDelegate>();

            internal Query(
                EntityContext context,
                QueryDelegate<TEntity> query)
            {
                this.context = context;
                this.query = query;
            }

            private async Task<object?> QueryAspectAsync<TAspect>(
                ILocator locator,
                CancellationToken cancellationToken)
                where TAspect : class
            {
                return await this.context
                    .QueryAspectAsync<TAspect>(locator, cancellationToken)
                    .ConfigureAwait(false);
            }

            /// <summary>
            /// Include an aspect in the query so that its value
            /// can later be accessed in EntityWithAspects.
            /// </summary>
            public Query IncludeAspect<TAspect>()
                where TAspect : class
            {
                //
                // Record that we need to query this aspect. We store a 
                // delegate (as opposed to just the aspect type) because
                // we won't have access to type information later.
                //
                if (!this.aspects.ContainsKey(typeof(TAspect)))
                {
                    this.aspects.Add(typeof(TAspect), QueryAspectAsync<TAspect>);
                }

                return this;
            }

            /// <summary>
            /// Derive an aspect from another aspect.
            /// </summary>
            public Query IncludeAspect<TAspect, TInputAspect>(
                Func<TInputAspect?, TAspect?> func)
                where TInputAspect : class
                where TAspect : class
            {
                //
                // Make sure we query the input aspect.
                //
                IncludeAspect<TInputAspect>();

                this.derivedAspects[typeof(TAspect)] = values =>
                {
                    TInputAspect? input = null;
                    if (values.TryGetValue(typeof(TInputAspect), out var value))
                    {
                        input = value as TInputAspect;
                    }

                    return func(input);
                };

                return this;
            }

            public async Task<EntityQueryResult<TEntity>> ExecuteAsync(
                CancellationToken cancellationToken)
            {
                //
                // Query entities.
                //
                var entities = await this
                    .query(cancellationToken)
                    .ConfigureAwait(false);

                //
                // Kick of asynchronous queries for each aspects and entity.
                //
                return new EntityQueryResult<TEntity>(await Task.WhenAll(entities
                    .Select(e => EntityQueryResult<TEntity>.Item.CreateAsync(
                        e,
                        this.aspects.ToDictionary(
                            item => item.Key,
                            item => item.Value(e.Locator, cancellationToken)),
                        this.derivedAspects))
                    .ToList()));
            }
        }
    }

    /// <summary>
    /// Collection of entities.
    /// </summary>
    public sealed class EntityQueryResult<TEntity> 
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