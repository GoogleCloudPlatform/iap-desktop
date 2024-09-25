using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.EntityModel
{


    //public interface IEntityContainer
    //{
    //    /// <summary>
    //    /// Types of locators that this provider supports.
    //    /// Types must be derived from ILocator.
    //    /// </summary>
    //    public ICollection<Type> SupportedLocatorTypes { get; }

    //    Task<ICollection<IEntity>> ListChildEntitiesAsync(
    //        ILocator locator,
    //        CancellationToken cancellationToken);
    //}

    ////TODO: Strongly type. Return nullable?

    //// InstanceState, Icon, InstanceDetails, ConnectionSettings
    //// ConectionState (observable)
    //public interface IEntityAspectProvider
    //{
    //    public ICollection<Type> SupportedLocatorTypes { get; }
    //    public ICollection<Type> SupportedAspects { get; }
    //}

    //public interface IEntityAspectProvider<TAspect> : IEntityAspectProvider
    //{
    //    Task<TAspect> GetAspectAsync(
    //        ILocator locator,
    //        CancellationToken cancellationToken);
    //}
}
