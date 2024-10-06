using Google.Solutions.Apis.Locator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
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

            public AspectQuery<TEntity> Search<TQuery>(TQuery query)
            {
                throw new NotImplementedException();
            }

            public AspectQuery<TEntity> ByAncestor(ILocator locator)
            {
                throw new NotImplementedException();
            }
        }

        public class AspectQuery<TEntity> // TODO: test
            where TEntity : IEntity<ILocator>
        {
            private readonly EntityContext context;
            private readonly Func<Task<ICollection<TEntity>>> queryFunc;
            private readonly Dictionary<Type, Func<ILocator, CancellationToken, Task<object?>>> aspectQueries;


            private async Task<object?> QueryAspectAsync<TAspect>(
                ILocator locator,
                CancellationToken cancellationToken)
                where TAspect : class
            {
                return await this.context
                    .QueryAspectAsync<TAspect>(locator, cancellationToken)
                    .ConfigureAwait(false);
            }

            public AspectQuery<TEntity> IncludeAspect<TAspect>()
                where TAspect : class
            {
                if (!this.aspectQueries.ContainsKey(typeof(TAspect)))
                {
                    this.aspectQueries.Add(typeof(TAspect), QueryAspectAsync<TAspect>);
                }

                return this;
            }

            public AspectQuery<TEntity> Include<TAspect, TInputAspect>(
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

            public async Task<ICollection<EntityProjection<TEntity>>> ExecuteAsync(
                CancellationToken cancellationToken)
            {
                var entities = await this.queryFunc().ConfigureAwait(false);

                foreach (var entity in entities)
                {
                    foreach (var aspectQuery in this.aspectQueries.Values)
                    {
                        await aspectQuery(entity.Locator, cancellationToken).ConfigureAwait(false);
                    }
                }

                // 1. Perform the query or expansion
                // 2. Query all aspects in parallel

                throw new NotImplementedException();
            }
        }

        public class EntityProjection<TEntity>
            where TEntity : IEntity<ILocator>
        {
            public TAspect Get<TAspect>()
            {
                throw new NotImplementedException();
            }
        }
    }
}