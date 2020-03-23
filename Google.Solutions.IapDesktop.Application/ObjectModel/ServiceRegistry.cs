using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{

    public class ServiceRegistry : IServiceProvider
    {
        private readonly IDictionary<Type, object> singletons = new Dictionary<Type, object>();
        private readonly IDictionary<Type, Func<object>> transients = new Dictionary<Type, Func<object>>();

        private TService CreateInstance<TService>()
        {
            var constructorWithServiceProvider = typeof(TService).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IServiceProvider) },
                null);
            if (constructorWithServiceProvider != null)
            {
                return (TService)Activator.CreateInstance(typeof(TService), (IServiceProvider)this);
            }

            var defaultConstructor = typeof(TService).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[0],
                null);
            if (defaultConstructor != null)
            {
                return (TService)Activator.CreateInstance(typeof(TService));
            }

            throw new UnknownServiceException(
                $"Class {typeof(TService).Name} lacks a suitable constructor to serve as service");
        }

        public void AddSingleton<TService>(TService singleton)
        {
            this.singletons[typeof(TService)] = singleton;
        }

        public void AddSingleton<TService>()
        {
            this.singletons[typeof(TService)] = CreateInstance<TService>();
        }

        public void AddSingleton<TService, TServiceClass>()
        {
            this.singletons[typeof(TService)] = CreateInstance<TServiceClass>();
        }

        public void AddTransient<TService>()
        {
            this.transients[typeof(TService)] = () => CreateInstance<TService>();
        }

        public object GetService(Type serviceType)
        {
            if (this.singletons.TryGetValue(serviceType, out object singleton))
            {
                return singleton;
            }
            else if (this.transients.TryGetValue(serviceType, out Func<object> transientFactory))
            {
                return transientFactory();
            }
            else
            {
                throw new UnknownServiceException(serviceType.Name);
            }
        }
    }
    public static class ServiceProviderExtensions
    {
        public static TService GetService<TService>(this IServiceProvider provider)
        {
            return (TService)provider.GetService(typeof(TService));
        }
    }

    [Serializable]
    public class UnknownServiceException : ApplicationException
    {
        protected UnknownServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public UnknownServiceException(string service) : base(service)
        {
        }
    }
}
