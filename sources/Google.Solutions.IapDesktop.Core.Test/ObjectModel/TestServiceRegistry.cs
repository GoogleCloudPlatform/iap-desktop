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

using Google.Solutions.Common.Runtime;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.Test.ObjectModel
{
    [TestFixture]
    public class TestServiceRegistry
    {
        public class ServiceWithDefaultConstructor
        {
        }

        public class ServiceWithServiceProviderConstructor
        {
            public IServiceProvider? Provider { get; }

            public ServiceWithServiceProviderConstructor()
            {
                this.Provider = null;
            }

            public ServiceWithServiceProviderConstructor(IServiceProvider provider)
            {
                this.Provider = provider;
            }
        }

        public class ServiceWithIncompatibleConstructor
        {
            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
            public ServiceWithIncompatibleConstructor(string s)
            {
            }
        }

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

            Assert.That(registry.GetService<ServiceWithDefaultConstructor>(), Is.SameAs(singleton));
        }

        //---------------------------------------------------------------------
        // Transient services.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTransientHasDefaultConstructor_ThenGetServiceReturnsNewInstance()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            Assert.That(registry.GetService<ServiceWithDefaultConstructor>(), Is.Not.Null);
        }

        [Test]
        public void WhenTransientHasSericeProviderConstructor_ThenGetServiceReturnsNewInstance()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceProviderConstructor>();
            Assert.That(service, Is.Not.Null);
            Assert.That(service.Provider, Is.SameAs(registry));

        }

        [Test]
        public void WhenTransientHasSericeCategoryProviderConstructor_ThenGetServiceReturnsNewInstance()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceCategoryProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceCategoryProviderConstructor>();
            Assert.That(service, Is.Not.Null);
            Assert.That(service.Provider, Is.SameAs(registry));
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
        // Custom constructor.
        //---------------------------------------------------------------------

        private class ServiceWithRecursiveConstructor
        {
            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
            public ServiceWithRecursiveConstructor(ServiceWithRecursiveConstructor obj)
            {
            }
        }

        private class ServiceWithPartiallySatisfiedConstructor
        {
            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
            public ServiceWithPartiallySatisfiedConstructor(ServiceWithDefaultConstructor obj, int i)
            {
            }
        }

        private class ServiceWithSatisfiedConstructor
        {
            public ServiceWithSatisfiedConstructor(
                Service<ServiceWithDefaultConstructor> obj,
                ServiceWithServiceProviderConstructor obj2)
            {
                Assert.That(obj, Is.Not.Null);
                Assert.That(obj2, Is.Not.Null);
            }

            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
            public ServiceWithSatisfiedConstructor(
                ServiceWithDefaultConstructor obj)
            {
                Assert.Fail("Wrong constructor");
            }

            public ServiceWithSatisfiedConstructor()
            {
                Assert.Fail("Wrong constructor");
            }
        }

        [Test]
        public void WhenSingletonServiceOnlyHasRecursiveConstructor_ThenAddSingletonThrowsException()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<ServiceWithRecursiveConstructor>();

            Assert.Throws<UnknownServiceException>(
                () => registry.GetService<ServiceWithRecursiveConstructor>());
        }

        [Test]
        public void WhenTransientServiceOnlyHasRecursiveConstructor_ThenGetServiceThrowsException()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithRecursiveConstructor>();

            Assert.Throws<UnknownServiceException>(
                () => registry.GetService<ServiceWithRecursiveConstructor>());
        }

        [Test]
        public void WhenTransientServiceOnlyHasPartiallySatisfiedConstructor_ThenGetServiceThrowsException()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithPartiallySatisfiedConstructor>();

            Assert.Throws<UnknownServiceException>(
                () => registry.GetService<ServiceWithRecursiveConstructor>());
        }

        [Test]
        public void WhenTransientServiceOnlyHasMultipleSatisfiedConstructors_ThenGetServiceUsesMostParameters()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();
            registry.AddTransient<ServiceWithSatisfiedConstructor>();

            Assert.That(registry.GetService<ServiceWithSatisfiedConstructor>(), Is.Not.Null);
        }

        //---------------------------------------------------------------------
        // Decorator.
        //---------------------------------------------------------------------

        [Test]
        public void WhenServiceIsDecorated_ThenGetServiceReturnsFactory()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            var service = registry.GetService<IActivator<ServiceWithDefaultConstructor>>();
            Assert.That(service, Is.Not.Null);
            Assert.That(service, Is.InstanceOf<Service<ServiceWithDefaultConstructor>>());
            Assert.That(service.Activate(), Is.Not.Null);

            var obj1 = service.Activate();
            var obj2 = service.Activate();

            Assert.That(obj1, Is.Not.SameAs(obj2));
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
            Assert.That(child.GetService<ServiceWithDefaultConstructor>(), Is.Not.Null);
        }

        [Test]
        public void WhenRegistryHasNoParent_ThenRootRegistryReturnsThis()
        {
            var registry = new ServiceRegistry();
            Assert.That(registry.RootRegistry, Is.SameAs(registry));
        }

        [Test]
        public void WhenRegistryHasParent_ThenRootRegistryReturnsParent()
        {
            var parent = new ServiceRegistry();
            var child = new ServiceRegistry(parent);
            var grandChild = new ServiceRegistry(child);
            Assert.That(grandChild.RootRegistry, Is.SameAs(parent));
        }

        [Test]
        public void WhenRegistryHasParent_ThenRegistrationsReturnsAllServices()
        {
            var parent = new ServiceRegistry();
            parent.AddSingleton<ServiceWithDefaultConstructor>();

            var child = new ServiceRegistry(parent);
            child.AddTransient<ServiceWithServiceProviderConstructor>();

            Assert.That(parent.Registrations.Count, Is.EqualTo(1));
            Assert.That(child.Registrations.Count, Is.EqualTo(2));

            var registrations = child.Registrations;

            Assert.That(
                registrations[typeof(ServiceWithDefaultConstructor)], Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(
                registrations[typeof(ServiceWithServiceProviderConstructor)], Is.EqualTo(ServiceLifetime.Transient));
        }

        //---------------------------------------------------------------------
        // Categories.
        //---------------------------------------------------------------------

        public interface ICategory
        { }

        public class FirstServiceImplementingCategory : ICategory
        { }

        public class SecondServiceImplementingCategory : ICategory
        { }

        public class ServiceWithServiceCategoryProviderConstructor
        {
            public IServiceCategoryProvider Provider { get; }

            public ServiceWithServiceCategoryProviderConstructor(IServiceCategoryProvider provider)
            {
                this.Provider = provider;
            }
        }

        [Test]
        public void WhenCategoryUnknown_ThenGetServicesByCategoryReturnsEmptyEnum()
        {
            var registry = new ServiceRegistry();
            var services = registry.GetServicesByCategory<ICloneable>();

            Assert.That(services, Is.Not.Null);
            Assert.That(services, Is.Empty);
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
            Assert.That(services, Is.Not.Null);
            Assert.That(services.Count(), Is.EqualTo(2));
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
            Assert.That(parentServices, Is.Not.Null);
            Assert.That(parentServices.Count(), Is.EqualTo(1));

            var childServices = childRegistry.GetServicesByCategory<ICategory>();
            Assert.That(childServices, Is.Not.Null);
            Assert.That(childServices.Count(), Is.EqualTo(2));
        }
    }
}
