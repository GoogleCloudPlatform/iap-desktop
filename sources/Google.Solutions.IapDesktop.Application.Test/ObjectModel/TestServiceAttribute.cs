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
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1040 // Avoid empty interfaces

namespace Google.Solutions.IapDesktop.Application.Test.ObjectModel
{
    [TestFixture]
    public class TestServiceAttribute : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // Register singleton service.
        //---------------------------------------------------------------------

        public interface ISingletonServiceInterface { }

        [Service(typeof(ISingletonServiceInterface), ServiceLifetime.Singleton)]
        public class SingletonServiceWithInterface : ISingletonServiceInterface
        {
        }

        [Service(ServiceLifetime.Singleton)]
        public class CounterService
        {
            public uint SingletonWithoutDelayCreationAttributeCount = 0;
            public uint SingletonWithDelayCreationAttributeCount = 0;
        }

        [Service(ServiceLifetime.Singleton)]
        public class SingletonWithoutDelayCreationAttribute
        {
            public SingletonWithoutDelayCreationAttribute(CounterService counter)
            {
                counter.SingletonWithoutDelayCreationAttributeCount++;
            }
        }

        [Service(ServiceLifetime.Singleton, DelayCreation = false)]
        public class SingletonWithDelayCreationAttribute
        {
            public SingletonWithDelayCreationAttribute(CounterService counter)
            {
                counter.SingletonWithDelayCreationAttributeCount++;
            }
        }

        [Test]
        public void WhenDelayCreationNotSet_ThenServiceIsCreatedLazily()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.AreEqual(
                0,
                registry.GetService<CounterService>().SingletonWithoutDelayCreationAttributeCount);

            registry.GetService<SingletonWithoutDelayCreationAttribute>();
            registry.GetService<SingletonWithoutDelayCreationAttribute>();

            Assert.AreEqual(
                1,
                registry.GetService<CounterService>().SingletonWithoutDelayCreationAttributeCount);
        }

        [Test]
        public void WhenDelayCreationIsFalse_ThenServiceIsCreatedEagerly()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.AreEqual(
                1,
                registry.GetService<CounterService>().SingletonWithDelayCreationAttributeCount);
        }

        [Test]
        public void WhenClassAnnotatedAsSingletonServiceWithInterface_ThenServiceIsRegistered()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.IsNotNull(registry.GetService<ISingletonServiceInterface>());
            Assert.AreSame(
                registry.GetService<ISingletonServiceInterface>(),
                registry.GetService<ISingletonServiceInterface>());
            Assert.Throws<UnknownServiceException>(
                () => registry.GetService<SingletonServiceWithInterface>());
        }

        [Service(ServiceLifetime.Singleton)]
        public class SingletonService : ISingletonServiceInterface
        {
        }

        [Test]
        public void WhenClassAnnotatedAsSingletonService_ThenServiceIsRegistered()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.IsNotNull(registry.GetService<SingletonService>());
            Assert.AreSame(
                registry.GetService<ISingletonServiceInterface>(),
                registry.GetService<ISingletonServiceInterface>());
        }

        //---------------------------------------------------------------------
        // Register transient service.
        //---------------------------------------------------------------------

        public interface ITransientServiceInterface { }

        [Service(typeof(ITransientServiceInterface), ServiceLifetime.Transient)]
        public class TransientServiceWithInterface : ITransientServiceInterface
        {
        }

        [Test]
        public void WhenClassAnnotatedAsTransientServiceWithInterface_ThenServiceIsRegistered()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.IsNotNull(registry.GetService<ITransientServiceInterface>());
            Assert.AreNotSame(
                registry.GetService<ITransientServiceInterface>(),
                registry.GetService<ITransientServiceInterface>());
            Assert.Throws<UnknownServiceException>(
                () => registry.GetService<TransientServiceWithInterface>());
        }

        [Service(ServiceLifetime.Transient)]
        public class TransientService
        {
        }

        [Test]
        public void WhenClassAnnotatedAsTransientService_ThenServiceIsRegistered()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.IsNotNull(registry.GetService<TransientService>());
            Assert.AreNotSame(
                registry.GetService<TransientService>(),
                registry.GetService<TransientService>());
        }

        //---------------------------------------------------------------------
        // Register service category.
        //---------------------------------------------------------------------

        public interface ICategory
        {
        }

        [Service]
        [ServiceCategory(typeof(ICategory))]
        public class TransientServiceImplementingCategory : ICategory
        { }

        public interface ITransientServiceWithInterface
        {
        }

        [Service(typeof(ITransientServiceWithInterface))]
        [ServiceCategory(typeof(ICategory))]
        public class TransientServiceWithInterfaceImplementingCategory : ITransientServiceWithInterface, ICategory
        { }

        [Test]
        public void WhenClassesAnnotatedAsServiceCategory_ThenServicesCanBeResolvedViaCategory()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            var services = registry.GetServicesByCategory<ICategory>();
            Assert.IsNotNull(services);
            Assert.AreEqual(2, services.Count());
            Assert.AreEqual(1, services.OfType<TransientServiceImplementingCategory>().Count());
            Assert.AreEqual(1, services.OfType<TransientServiceWithInterfaceImplementingCategory>().Count());
        }
    }
}
