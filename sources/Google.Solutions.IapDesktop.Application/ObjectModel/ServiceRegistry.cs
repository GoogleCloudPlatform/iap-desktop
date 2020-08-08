﻿//
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
        private readonly IDictionary<Type, SingletonStub> singletons = new Dictionary<Type, SingletonStub>();
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
            this.singletons[typeof(TService)] = new SingletonStub(singleton);
        }

        public void AddSingleton<TService>()
        {
            this.singletons[typeof(TService)] = new SingletonStub(CreateInstance<TService>());
        }

        public void AddSingleton<TService, TServiceClass>()
        {
            this.singletons[typeof(TService)] = new SingletonStub(CreateInstance<TServiceClass>());
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
            if (this.singletons.TryGetValue(serviceType, out SingletonStub singletonStub))
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
                throw new UnknownServiceException(serviceType.Name);
            }
        }

        public void AddExtensionAssembly(Assembly assembly)
        {
            //
            // First, register all transients. 
            //
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute &&
                    attribute.Lifetime == ServiceLifetime.Transient)
                {
                    this.transients[attribute.ServiceInterface ?? type] =
                        () => CreateInstance(type);
                }
            }

            //
            // Register singletons. Because transients are already registered,
            // it is ok if singletons instantiate transients in their constructor.
            //
            // As singletons might depend on another, we cannot instantiate them
            // in a single pass. Instead, register stubs in the first pass,
            // then force their instantiation in a second pass.
            //

            //
            // (1) Register stubs, but do not create instances yet.
            //
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute attribute &&
                    attribute.Lifetime == ServiceLifetime.Singleton)
                {
                    this.singletons[attribute.ServiceInterface ?? type] 
                        = new SingletonStub(() => CreateInstance(type));
                }
            }

            //
            // (2) Trigger stubs to create instances.
            //
            foreach (var stub in this.singletons.Values)
            {
                var instance = stub.Object;
                Debug.Assert(instance != null);
            }
        }

        private class SingletonStub
        {
            private object value;
            private readonly Func<object> factoryFunc;

            public SingletonStub(object value)
            {
                this.value = value;
            }

            public SingletonStub(Func<object> factoryFunc)
            {
                this.factoryFunc = factoryFunc;
            }

            public object Object
            {
                get
                {
                    //
                    // NB. This does not need to be thread-safe as the
                    // initialization happens synchronously,
                    // see CreateInstance().
                    //
                    if (this.value == null)
                    {
                        Debug.Assert(this.factoryFunc != null);
                        this.value = this.factoryFunc();
                        Debug.Assert(this.value != null);
                    }

                    return this.value;
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
