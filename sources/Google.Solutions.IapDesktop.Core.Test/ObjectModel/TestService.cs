//
// Copyright 2024 Google LLC
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

namespace Google.Solutions.IapDesktop.Core.Test.ObjectModel
{
    [TestFixture]
    public class TestService
    {
        public class ServiceWithDefaultConstructor
        {
        }

        [Test]
        public void WhenReferencedServiceUnknown_ThenThenGetServiceThrowsUnknownServiceException()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<Service<string>>();
            });
        }

        [Test]
        public void WhenReferencedServiceIsTransient_ThenGetServiceReturnsFactory()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            var service = registry.GetService<Service<ServiceWithDefaultConstructor>>();
            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Activate());
            Assert.AreNotSame(service.Activate(), service.Activate());
        }

        [Test]
        public void WhenReferencedServiceIsSingleton_ThenGetServiceReturnsFactory()
        {
            var registry = new ServiceRegistry();
            registry.AddSingleton<ServiceWithDefaultConstructor>();

            var service = registry.GetService<Service<ServiceWithDefaultConstructor>>();
            Assert.IsNotNull(service);
            Assert.IsNotNull(service.Activate());
            Assert.AreSame(service.Activate(), service.Activate());
        }
    }
}
