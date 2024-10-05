using Google.Solutions.Apis.Locator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{
    // TODO: Implement
    public static class EntityContextQueryExtensions
    {
        public static AspectQuery<TEntity> Search<TQuery, TEntity>(
            this EntityContext context,
            TQuery query,
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            throw new NotImplementedException();
        }

        public static AspectQuery<TEntity> Expand<TEntity>(
            this EntityContext context,
            ILocator locator,
            CancellationToken cancellationToken)
            where TEntity : IEntity
        {
            throw new NotImplementedException();
        }
    }

    public class AspectQuery<TEntity>
        where TEntity : IEntity
    {
        public AspectQuery<TEntity> Select<TAspect>()
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<EntityProjection<TEntity>>> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class EntityProjection<TEntity>
        where TEntity : IEntity
    {
        public TAspect Get<TAspect>()
        {
            throw new NotImplementedException();
        }
    }
}
