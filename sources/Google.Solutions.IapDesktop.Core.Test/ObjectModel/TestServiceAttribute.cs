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

using Google.Solutions.IapDesktop.Core.ObjectModel;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Core.Test.ObjectModel
{
    [TestFixture]
    public class TestServiceAttribute
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

            Assert.That(
                registry.GetService<CounterService>().SingletonWithoutDelayCreationAttributeCount, Is.EqualTo(0));

            registry.GetService<SingletonWithoutDelayCreationAttribute>();
            registry.GetService<SingletonWithoutDelayCreationAttribute>();

            Assert.That(
                registry.GetService<CounterService>().SingletonWithoutDelayCreationAttributeCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenDelayCreationIsFalse_ThenServiceIsCreatedEagerly()
        {
            var registry = new ServiceRegistry();
            registry.AddExtensionAssembly(Assembly.GetExecutingAssembly());

            Assert.That(
                registry.GetService<CounterService>().SingletonWithDelayCreationAttributeCount, Is.EqualTo(1));
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
            Assert.That(services.Count(), Is.EqualTo(2));
            Assert.That(services.OfType<TransientServiceImplementingCategory>().Count(), Is.EqualTo(1));
            Assert.That(services.OfType<TransientServiceWithInterfaceImplementingCategory>().Count(), Is.EqualTo(1));
        }
    }
}
