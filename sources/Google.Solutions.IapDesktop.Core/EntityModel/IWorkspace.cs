using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.ResourceModel
{

    public interface IWorkspace 
    {
        /// <summary>
        /// Set the active/selected item.
        /// </summary>
        Task SetActiveItemAsync(ILocator locator);

        /// <summary>
        /// Get the active/selected item
        /// </summary>
        Task<ILocator> GetActiveItemAsync();

        /// <summary>
        /// Check if this item might have child items.
        /// </summary>
        bool CanList(ILocator locator); // -> check if any provider offers to list

        bool CanGetAspect<TAspect>(ILocator locator);




        Task<ICollection<IEntity>> ListAsync(
            ILocator locator,
            CancellationToken cancellationToken);


        Task<ICollection<IEntity>> SearchAsync(
            string query,
            CancellationToken cancellationToken);

        Task<TAspect> GetAspectAsync<TAspect>(
            ILocator locator,
            CancellationToken cancellationToken);
    }

    class Workspace // : IWorkspace
    {
        public Workspace(IServiceCategoryProvider provider) 
        { }

        public Workspace(
            ICollection<IEntityContainer> entityProviders,
            ICollection<IEntityAspectProvider> aspectProviders) 
        { }


        void Register<TLocator, TEntity>(IEntityContainer<TLocator, TEntity> entityContainer)
            where TLocator : ILocator
            where TEntity : IEntity<TLocator>
        { }


        void Register(IEntityContainer entityContainer)
        { 
            // Make generic method, cann Register<>
        }
    }
}
