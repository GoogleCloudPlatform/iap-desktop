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

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestServiceRegistry
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
        public void WhenRequestingSingleton_ExistingInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            var singleton = new ServiceWithDefaultConstructor();
            registry.AddSingleton<ServiceWithDefaultConstructor>(singleton);

            Assert.AreSame(singleton, registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenRequestingTransientWithDefaultConstructor_NewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            Assert.IsNotNull(registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenRequestingTransientWithSericeProviderConstructor_NewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceProviderConstructor>();
            Assert.IsNotNull(service);
            Assert.AreSame(registry, service.provider);

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
    }
}
