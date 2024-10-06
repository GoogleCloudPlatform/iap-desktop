using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    // TODO: Implement
    public static class EntityContextQuery
    {
        /// <summary>
        /// Query entities of a certain type, or subtype thereof.
        /// </summary>
        public static EntityQuery<TEntity> Entities<TEntity>(
            this EntityContext context)
            where TEntity : IEntity<ILocator>
        {
            return new EntityQuery<TEntity>(context);
        }

        public class EntityQuery<TEntity>
            where TEntity : IEntity<ILocator>
        {
            private readonly EntityContext context;

            internal EntityQuery(EntityContext context)
            {
                this.context = context;
            }

            /// <summary>
            /// Query entities by performing a search.
            /// </summary>
            public AspectQuery<TEntity> Search<TQuery>(TQuery query)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Query entities by performing a wildcard search.
            /// </summary>
            public AspectQuery<TEntity> List()
            {
                return Search(AnyQuery.Instance);
            }

            /// <summary>
            /// Query entities by parent locator.
            /// </summary>
            /// <param name="locator">Locator of parent entity</param>
            public AspectQuery<TEntity> ByAncestor(ILocator locator)
            {
                throw new NotImplementedException();
            }
        }

        public class AspectQuery<TEntity> // TODO: test
            where TEntity : IEntity<ILocator>
        {
            private readonly EntityContext context;
            private readonly Func<Task<ICollection<TEntity>>> queryDelegate;
            private readonly Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>> aspectQueryDelegates;

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
            public AspectQuery<TEntity> IncludeAspect<TAspect>()
                where TAspect : class
            {
                //
                // Record that we need to query this aspect. We store a 
                // delegate (as opposed to just the aspect type) because
                // we won't have access to type information later.
                //
                if (!this.aspectQueryDelegates.ContainsKey(typeof(TAspect)))
                {
                    this.aspectQueryDelegates.Add(typeof(TAspect), QueryAspectAsync<TAspect>);
                }

                return this;
            }

            /// <summary>
            /// Derive an aspect from another aspect.
            /// </summary>
            public AspectQuery<TEntity> IncludeAspect<TAspect, TInputAspect>(
                Func<TInputAspect, TAspect> func)
                where TInputAspect : class
                where TAspect : class
            {
                //
                // Make sure we query the input aspect.
                //
                IncludeAspect<TInputAspect>();

                throw new NotImplementedException();
            }

            public async Task<ICollection<EntityWithAspects<TEntity>>> ExecuteAsync(
                CancellationToken cancellationToken)
            {
                //
                // Query entities.
                //
                var entities = await this
                    .queryDelegate()
                    .ConfigureAwait(false);

                //
                // Kick of asynchronous queries for each aspects and entity.
                //
                return await Task.WhenAll(entities
                    .Select(e => EntityWithAspects<TEntity>.CreateAsync(
                        e,
                        this.aspectQueryDelegates.ToDictionary(
                            item => item.Key,
                            item => item.Value(e.Locator, cancellationToken))))
                    .ToList());
            }
        }

    }

    /// <summary>
    /// An entity with associated aspects.
    /// </summary>
    public class EntityWithAspects<TEntity>
        where TEntity : IEntity<ILocator>
    {
        private readonly Dictionary<Type, Task<object?>> aspectQueryTasks;

        internal static async Task<EntityWithAspects<TEntity>> CreateAsync(
            TEntity entity,
            Dictionary<Type, Task<object?>> aspectQueryTasks)
        {
            await Task
                .WhenAll(aspectQueryTasks.Values)
                .ConfigureAwait(false);

            return new EntityWithAspects<TEntity>(entity, aspectQueryTasks);
        }

        private EntityWithAspects(TEntity entity, Dictionary<Type, Task<object?>> aspectQueryTasks)
        {
            this.Entity = entity;
            this.aspectQueryTasks = aspectQueryTasks;
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
            if (this.aspectQueryTasks.TryGetValue(typeof(TAspect), out var task))
            {
                Debug.Assert(task.IsCompleted);
                return task.Result as TAspect;
            }
            else
            {
                return null;
            }
        }
    }
}