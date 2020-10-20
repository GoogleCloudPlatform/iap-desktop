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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    public class TestServiceRegistry : FixtureBase
    {
        public class ServiceWithDefaultConstructor
        {
        }

        public class ServiceWithServiceProviderConstructor
        {
            public IServiceProvider provider;

            public ServiceWithServiceProviderConstructor()
            {
                this.provider = null;
            }

            public ServiceWithServiceProviderConstructor(IServiceProvider provider)
            {
                this.provider = provider;
            }
        }

        public class ServiceWithIncompatibleConstructor
        {
            public ServiceWithIncompatibleConstructor(string s)
            {
            }
        }

        public interface ICategory
        { }

        public class FirstServiceImplementingCategory : ICategory
        { }

        public class SecondServiceImplementingCategory : ICategory
        { }

        //---------------------------------------------------------------------
        // Singleton services.
        //---------------------------------------------------------------------

        [Test]
        public void WhenServiceUnknown_ThenThenGetServiceThrowsUnknownServiceException()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<string>();
            });
        }

        [Test]
        public void WhenSingletonRegistered_ThenThenGetServiceReturnsExistingInstance()
        {
            var registry = new ServiceRegistry();
            var singleton = new ServiceWithDefaultConstructor();
            registry.AddSingleton<ServiceWithDefaultConstructor>(singleton);

            Assert.AreSame(singleton, registry.GetService<ServiceWithDefaultConstructor>());
        }

        //---------------------------------------------------------------------
        // Transient services.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTransientHasDefaultConstructor_ThenGetServiceReturnsNewInstance()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            Assert.IsNotNull(registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenTransientHasSericeProviderConstructor_ThenGetServiceReturnsNewInstance()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceProviderConstructor>();
            Assert.IsNotNull(service);
            Assert.AreSame(registry, service.provider);

        }

        [Test]
        public void WhenTransientHasIncompatibleConstructor_ThenGetServiceThrowsUnknownServiceException()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<ServiceWithIncompatibleConstructor>();
            });
        }

        //---------------------------------------------------------------------
        // Nesting.
        //---------------------------------------------------------------------

        [Test]
        public void WhenServiceUnknown_ThenGetServiceQueriesParent()
        {
            var parent = new ServiceRegistry();
            parent.AddTransient<ServiceWithDefaultConstructor>();

            var child = new ServiceRegistry(parent);
            Assert.IsNotNull(child.GetService<ServiceWithDefaultConstructor>());
        }

        //---------------------------------------------------------------------
        // Categories.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCategoryUnknown_ThenGetServicesByCategoryReturnsEmptyEnum()
        {
            var registry = new ServiceRegistry();
            var services = registry.GetServicesByCategory<ICloneable>();

            Assert.IsNotNull(services);
            CollectionAssert.IsEmpty(services);
        }

        [Test]
        public void WhenCategoryIsClass_ThenAddServiceToCategoryThrowsArgumentException()
        {
            var registry = new ServiceRegistry();
            
            Assert.Throws<ArgumentException>(() => registry.AddServiceToCategory<Uri, Uri>());
        }

        [Test]
        public void WhenServiceUnknown_ThenAddServiceToCategoryThrowsUnknownServiceException()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(
                () => registry.AddServiceToCategory<ICategory, FirstServiceImplementingCategory>());
        }

        [Test]
        public void WhenServiceDoesNotImplementCategory_ThenGetServicesByCategoryThrowsInvalidCastException()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();
            registry.AddServiceToCategory<ICategory, ServiceWithDefaultConstructor>();

            Assert.Throws<InvalidCastException>(
                () => registry.GetServicesByCategory<ICategory>().ToArray());
        }

        [Test]
        public void WhenTwoServiceImplementCategory_ThenGetServicesByCategoryReturnsBoth()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<FirstServiceImplementingCategory>();
            registry.AddTransient<SecondServiceImplementingCategory>();

            registry.AddServiceToCategory<ICategory, FirstServiceImplementingCategory>();
            registry.AddServiceToCategory<ICategory, SecondServiceImplementingCategory>();

            var services = registry.GetServicesByCategory<ICategory>();
            Assert.IsNotNull(services);
            Assert.AreEqual(2, services.Count());
        }

        [Test]
        public void WhenParentAndChildHaveServiceThatImplementCategory_ThenGetServicesByCategoryReturnsBoth()
        {
            var parentRegistry = new ServiceRegistry();
            parentRegistry.AddSingleton<FirstServiceImplementingCategory>();
            parentRegistry.AddServiceToCategory<ICategory, FirstServiceImplementingCategory>();

            var childRegistry = new ServiceRegistry(parentRegistry);
            childRegistry.AddTransient<SecondServiceImplementingCategory>();
            childRegistry.AddServiceToCategory<ICategory, SecondServiceImplementingCategory>();

            var parentServices = parentRegistry.GetServicesByCategory<ICategory>();
            Assert.IsNotNull(parentServices);
            Assert.AreEqual(1, parentServices.Count());

            var childServices = childRegistry.GetServicesByCategory<ICategory>();
            Assert.IsNotNull(childServices);
            Assert.AreEqual(2, childServices.Count());
        }
    }
}
