//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

#pragma warning disable CA1032 // Implement standard exception constructors

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    /// <summary>
    /// Registry that allows lookup and creation of object by type, similar to a 
    /// "Kernel" in some IoC frameworks.
    /// 
    /// ServiceRegistries can be nested. In this case, a registry first queries its
    /// own services before delegating to the parent registry.
    /// 
    /// Service registration is not thread-safe and expected to happen during
    /// startup. Once all services have been registered, service lookup is
    /// thread-safe.
    /// </summary>
    public class ServiceRegistry : IServiceCategoryProvider, IServiceProvider
    {
        private readonly ServiceRegistry parent;
        private readonly IDictionary<Type, SingletonStub> singletons = new Dictionary<Type, SingletonStub>();
        private readonly IDictionary<Type, Func<object>> transients = new Dictionary<Type, Func<object>>();

        // Categories map a category type to a list of service types. They can be used if there
        // are multiple implementations for a common function.
        private readonly IDictionary<Type, IList<Type>> categories = new Dictionary<Type, IList<Type>>();

        public ServiceRegistry()
        {
            this.parent = null;
        }

        public ServiceRegistry(ServiceRegistry parent)
        {
            this.parent = parent;
        }

        internal ServiceRegistry RootRegistry => this.parent != null
            ? this.parent.RootRegistry
            : this;

        //---------------------------------------------------------------------
        // Service registration and instantiation.
        //---------------------------------------------------------------------

        private object CreateInstance(Type serviceType)
        {
            //
            // Check for ctor(IServiceProvider).
            //
            var constructorWithServiceProvider = serviceType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IServiceProvider) },
                null);
            if (constructorWithServiceProvider != null)
            {
                return Activator.CreateInstance(serviceType, (IServiceProvider)this);
            }

            //
            // Check for ctor(IServiceCategoryProvider).
            //
            var constructorWithServiceCategoryProvider = serviceType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IServiceCategoryProvider) },
                null);
            if (constructorWithServiceCategoryProvider != null)
            {
                return Activator.CreateInstance(serviceType, (IServiceCategoryProvider)this);
            }

            //
            // Check other constructors and see if there is one for which all
            // parameters can be bound to a service. Analyze the one with the most
            // parameters first.
            //
            bool IsSupportedConstructorArgumentType(Type t)
            {
                if (t == serviceType)
                {
                    // Don't allow recursion
                    return false;
                }

                if (IsServiceRegistered(t))
                {
                    return true;
                }
                else if (t.IsGenericType &&
                    t.GetGenericTypeDefinition() == typeof(Service<>) &&
                    t.GenericTypeArguments.Length == 1 &&
                    IsServiceRegistered(t.GenericTypeArguments[0]))

                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            foreach (var constructor in serviceType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length))
            {
                if (constructor
                    .GetParameters()
                    .Select(p => p.ParameterType)
                    .All(t => IsSupportedConstructorArgumentType(t)))
                {
                    // We have services for all parameters.
                    var parameterValues = constructor
                        .GetParameters()
                        .Select(p => GetService(p.ParameterType))
                        .ToArray();

                    return Activator.CreateInstance(
                        serviceType,
                        parameterValues);
                }
                else
                {
#if DEBUG
                    Debug.WriteLine($"ServiceRegistry: {serviceType} has an unsuitable ctor: {constructor}");

                    foreach (var t in constructor
                        .GetParameters()
                        .Select(p => p.ParameterType)
                        .Where(t => !IsSupportedConstructorArgumentType(t))
                        .ToList())
                    {
                        Debug.WriteLine($"  > Unsupported argument type: {t}");
                    }
#endif

                    //
                    // There is at least one parameter that we do not
                    // have a suitable service for.
                    //
                }
            }

            throw new UnknownServiceException(
                $"Class {serviceType.Name} lacks a suitable constructor to serve as service");
        }

        private TService CreateInstance<TService>()
        {
            return (TService)CreateInstance(typeof(TService));
        }

        //---------------------------------------------------------------------
        // Singleton registration.
        //---------------------------------------------------------------------

        private void AddSingleton(Type singletonType, SingletonStub stub)
        {
            this.singletons[singletonType] = stub;
        }

        public void AddSingleton<TService>(TService singleton)
        {
            AddSingleton(typeof(TService), new SingletonStub(singleton));
        }

        public void AddSingleton<TService, TServiceClass>()
        {
            AddSingleton(typeof(TService), new SingletonStub(() => CreateInstance<TServiceClass>()));
        }

        public void AddSingleton<TService>()
        {
            AddSingleton<TService, TService>();
        }

        //---------------------------------------------------------------------
        // Transient registration.
        //---------------------------------------------------------------------

        private void AddTransient(Type serviceType, Type implementationType)
        {
            this.transients[serviceType] = () => CreateInstance(implementationType);
        }

        public void AddTransient<TService>()
        {
            AddTransient(typeof(TService), typeof(TService));
        }

        public void AddTransient<TService, TServiceClass>()
        {
            AddTransient(typeof(TService), typeof(TServiceClass));
        }

        //---------------------------------------------------------------------
        // Lookup.
        //---------------------------------------------------------------------

        private bool IsServiceRegistered(Type serviceType)
        {
            return this.singletons.ContainsKey(serviceType) ||
                   this.transients.ContainsKey(serviceType) ||
                   (this.parent != null && this.parent.IsServiceRegistered(serviceType));
        }

        public object GetService(Type serviceType)
        {
            if (serviceType.IsGenericType &&
                serviceType.GetGenericTypeDefinition() == typeof(Service<>))
            {
                Debug.Assert(serviceType.GetGenericArguments().Length == 1);
                var actualServiceType = serviceType.GetGenericArguments()[0];

                //
                // Verify that the service exists so that we don't get any
                // surprises later.
                //
                if (!IsServiceRegistered(actualServiceType))
                {
                    throw new UnknownServiceException(
                        $"Unknown service: {actualServiceType.Name}");
                }

                //
                // Return a factory instead of the actual service object.
                // This doesn't make a real difference for singletons, but
                // for transients it lets clients delay object creation
                // (and hence, lookup of dependencies).
                //
                // NB. Service<> is a valid service, so we can create it using
                // just like a normal transient.
                //
                return CreateInstance(serviceType);
            }
            else if (this.singletons.TryGetValue(serviceType, out var singletonStub))
            {
                return singletonStub.Object;
            }
            else if (this.transients.TryGetValue(serviceType, out Func<object> transientFactory))
            {
                return transientFactory();
            }
            else if (this.parent != null)
            {
                return this.parent.GetService(serviceType);
            }
            else
            {
                throw new UnknownServiceException(
                    $"Unknown service: {serviceType.Name}");
            }
        }

        //---------------------------------------------------------------------
        // Categories.
        //---------------------------------------------------------------------

        public void AddServiceToCategory(Type categoryType, Type serviceType)
        {
            if (!categoryType.IsInterface)
            {
                throw new ArgumentException("Category must be an interface");
            }

            if (!this.singletons.ContainsKey(serviceType) &&
                !this.transients.ContainsKey(serviceType))
            {
                throw new UnknownServiceException(serviceType.Name);
            }

            if (this.categories.TryGetValue(categoryType, out var serviceTypes))
            {
                serviceTypes.Add(serviceType);
            }
            else
            {
                this.categories[categoryType] = new List<Type>()
                {
                    serviceType
                };
            }
        }

        public void AddServiceToCategory<TCategory, TService>()
        {
            AddServiceToCategory(typeof(TCategory), typeof(TService));
        }

        public IEnumerable<TCategory> GetServicesByCategory<TCategory>()
        {
            //
            // NB. Services registered for the same category in a lower layer
            // are not visible unless they are registered as "global".
            //

            //
            // Consider parent services.
            //
            IEnumerable<TCategory> services;
            if (this.parent != null)
            {
                services = this.parent.GetServicesByCategory<TCategory>();
            }
            else
            {
                services = Enumerable.Empty<TCategory>();
            }

            //
            // Consider own services.
            //
            if (this.categories.TryGetValue(typeof(TCategory), out var serviceTypes))
            {
                services = services.Concat(serviceTypes.Select(t => (TCategory)GetService(t)));
            }

            return services;
        }

        //---------------------------------------------------------------------
        // Extension assembly handling.
        //---------------------------------------------------------------------

        private ServiceRegistry GetServiceRegistryToRegisterWith(ServiceAttribute attribute)
        {
            // If it's visible globally, use the root registry.
            return attribute.Visibility == ServiceVisibility.Global
                ? this.RootRegistry
                : this;
        }

        public void AddExtensionAssembly(Assembly assembly)
        {
            //
            // NB. By default, services are registered in this service registry, making
            // them visible to this and lower layers.
            //
            // Services can optionally register as "global" to be registered in the
            // root registry. This makes them visible across all layers. Their dependencies
            // are still resolved in this layer -- not in the root layer.
            //

            //
            // (1) First, register all transients. 
            //
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute &&
                    attribute.Lifetime == ServiceLifetime.Transient)
                {
                    GetServiceRegistryToRegisterWith(attribute).AddTransient(
                        attribute.ServiceInterface ?? type,
                        type);
                }
            }

            //
            // (2) Register stubs, but do not create instances yet because they
            //     might depend on another and we don't know what the right order
            //     would have to be.
            //
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute &&
                    attribute.Lifetime == ServiceLifetime.Singleton)
                {
                    var stub = attribute.DelayCreation
                        ? new SingletonStub(() => CreateInstance(type))
                        : new SingletonStub(CreateInstance(type));

                    GetServiceRegistryToRegisterWith(attribute).AddSingleton(
                        attribute.ServiceInterface ?? type,
                        stub);
                }
            }

            //
            // (3) Register categories.
            //
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute &&
                    type.GetCustomAttribute<ServiceCategoryAttribute>() is ServiceCategoryAttribute categoryAttribute)
                {
                    GetServiceRegistryToRegisterWith(attribute).AddServiceToCategory(
                        categoryAttribute.Category,
                        attribute.ServiceInterface ?? type);
                }
            }
        }

        //---------------------------------------------------------------------
        // Helper classes.
        //---------------------------------------------------------------------

        private class SingletonStub
        {
            private readonly Lazy<object> factoryFunc;

            public SingletonStub(object value)
                : this(() => value)
            {
            }

            public SingletonStub(Func<object> factoryFunc)
            {
                this.factoryFunc = new Lazy<object>(
                    factoryFunc, 
                    LazyThreadSafetyMode.ExecutionAndPublication);
            }

            public object Object => this.factoryFunc.Value;
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
