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

        public class ServiceWithServiceProviderAndOtherConstructors
        {
            public IServiceProvider serviceProvider;

            public ServiceWithServiceProviderAndOtherConstructors(
                IServiceProvider serviceProvider)
            {
                this.serviceProvider = serviceProvider;
            }

            public ServiceWithServiceProviderAndOtherConstructors(
                ServiceWithDefaultConstructor serviceWithDefaultConstructor)
            {
                Assert.Fail();
            }

            public ServiceWithServiceProviderAndOtherConstructors(
                ServiceWithServiceProviderAndOtherConstructors parent,
                string other)
            {
                Assert.Fail();
            }
        }

        public class ServiceWithMultipleConstructors
        {
            public ServiceWithServiceProviderConstructor serviceWithServiceProviderConstructor;
            public ServiceWithDefaultConstructor serviceWithDefaultConstructor;

            public ServiceWithMultipleConstructors(
                ServiceWithServiceProviderConstructor serviceWithServiceProviderConstructor,
                ServiceWithDefaultConstructor serviceWithDefaultConstructor)
            {
                this.serviceWithServiceProviderConstructor = serviceWithServiceProviderConstructor;
                this.serviceWithDefaultConstructor = serviceWithDefaultConstructor;
            }

            public ServiceWithMultipleConstructors(
                ServiceWithMultipleConstructors parent,
                ServiceWithServiceProviderConstructor serviceWithServiceProviderConstructor,
                ServiceWithDefaultConstructor serviceWithDefaultConstructor)
            {
                Assert.Fail();
            }

            public ServiceWithMultipleConstructors(
                ServiceWithDefaultConstructor serviceWithDefaultConstructor)
            {
                this.serviceWithDefaultConstructor = serviceWithDefaultConstructor;
            }

            public ServiceWithMultipleConstructors(
                ServiceWithDefaultConstructor serviceWithDefaultConstructor,
                bool ignored)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void WhenRequestingUnknownService_UnknownServiceExceptionIsThrown()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<string>();
            });
        }

        [Test]
        public void WhenRequestingUnknownService_ThenParentRegistryIsQueried()
        {
            var parent = new ServiceRegistry();
            parent.AddTransient<ServiceWithDefaultConstructor>();

            var child = new ServiceRegistry(parent);
            Assert.IsNotNull(child.GetService<ServiceWithDefaultConstructor>());
        }

        //---------------------------------------------------------------------
        // Singletons.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRequestingSingleton_ExistingInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            var singleton = new ServiceWithDefaultConstructor();
            registry.AddSingleton<ServiceWithDefaultConstructor>(singleton);

            Assert.AreSame(singleton, registry.GetService<ServiceWithDefaultConstructor>());
        }

        //---------------------------------------------------------------------
        // Transients.
        //---------------------------------------------------------------------

        public class ServiceWithIncompatibleConstructor
        {
            public ServiceWithIncompatibleConstructor(string s)
            {
            }
        }

        [Test]
        public void WhenRequestingTransientWithIncompatibleConstructor_UnknownServiceExceptionIsThrown()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<ServiceWithIncompatibleConstructor>();
            });
        }

        [Test]
        public void WhenRequestingTransientWithDefaultConstructor_ThenNewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            Assert.IsNotNull(registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenRequestingTransientWithSericeProviderConstructor_ThenNewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceProviderConstructor>();
            Assert.IsNotNull(service);
            Assert.AreSame(registry, service.provider);

        }

        [Test]
        public void WhenRequestingTransientWithSericeProviderAndOtherConstructors_ThenNewInstanceIsCreatedUsingServiceProviderConstructor()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();
            registry.AddTransient<ServiceWithServiceProviderAndOtherConstructors>();

            var service = registry.GetService<ServiceWithServiceProviderAndOtherConstructors>();
            Assert.IsNotNull(service);
            Assert.AreSame(registry, service.serviceProvider);
        }

        [Test]
        public void WhenRequestingTransientWithMultipleConstructors_ThenNewInstanceIsCreatedUsingConstructorWithMostParameters()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();
            registry.AddTransient<ServiceWithMultipleConstructors>();

            var service = registry.GetService<ServiceWithMultipleConstructors>();
            Assert.IsNotNull(service);
            Assert.IsNotNull(service.serviceWithDefaultConstructor);
            Assert.IsNotNull(service.serviceWithServiceProviderConstructor);
        }
    }
}
