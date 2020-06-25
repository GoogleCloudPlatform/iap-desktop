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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    /// <summary>
    /// Registry that allows lookup and creation of object by type, similar to a 
    /// "Kernel" in some IoC frameworks.
    /// 
    /// ServiceRegistries can be nested. In this case, a registry first queries its
    /// own services before delegating to the parent registry.
    /// </summary>
    public class ServiceRegistry : IServiceProvider
    {
        private readonly ServiceRegistry parent;
        private readonly IDictionary<Type, object> singletons = new Dictionary<Type, object>();
        private readonly IDictionary<Type, Func<object>> transients = new Dictionary<Type, Func<object>>();

        public ServiceRegistry()
        {
            this.parent = null;
        }

        public ServiceRegistry(ServiceRegistry parent)
        {
            this.parent = parent;
        }

        private object CreateInstance(Type serviceType)
        {
            var constructorWithServiceProvider = serviceType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(IServiceProvider) },
                null);
            if (constructorWithServiceProvider != null)
            {
                return Activator.CreateInstance(serviceType, (IServiceProvider)this);
            }

            var defaultConstructor = serviceType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Array.Empty<Type>(),
                null);
            if (defaultConstructor != null)
            {
                return Activator.CreateInstance(serviceType);
            }

            throw new UnknownServiceException(
                $"Class {serviceType.Name} lacks a suitable constructor to serve as service");
        }

        private TService CreateInstance<TService>()
        {
            return (TService)CreateInstance(typeof(TService));
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

        public void AddTransient<TService, TServiceClass>()
        {
            this.transients[typeof(TService)] = () => CreateInstance<TServiceClass>();
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
            else if (this.parent != null)
            {
                return this.parent.GetService(serviceType);
            }
            else
            {
                throw new UnknownServiceException(serviceType.Name);
            }
        }

        public void AddExtensionAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute)
                {
                    if (attribute.Lifetime == ServiceLifetime.Singleton)
                    {
                        var instance = CreateInstance(type);

                        this.singletons[attribute.ServiceInterface ?? type] = instance;
                    }
                    else
                    {
                        this.transients[attribute.ServiceInterface ?? type] = 
                            () => CreateInstance(type);
                    }
                }
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
