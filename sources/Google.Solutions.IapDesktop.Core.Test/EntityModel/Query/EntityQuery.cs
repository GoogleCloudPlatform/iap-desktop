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


using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.EntityModel;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.EntityModel.Query
{
    [TestFixture]
    public class EntityQuery
    {
        public class CarLocator : ILocator
        {
            public string ResourceType => "car";
        }

        public class Car : IEntity<CarLocator>
        {
            public string DisplayName { get; }

            public CarLocator Locator { get; }

            public Car(string displayName, CarLocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        //----------------------------------------------------------------------
        // IncludeAspect.
        //----------------------------------------------------------------------

        [Test]
        public async Task IncludeAspect_WhenAspectNotSupported()
        {
            var searcher = new Mock<IEntitySearcher<WildcardQuery, Car>>();
            searcher
                .Setup(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] { new Car("car-1", new CarLocator()) });
            var context = new EntityContext.Builder()
                .AddSearcher(searcher.Object)
                .Build();

            var cars = await context.Entities<Car>()
                .List()
                .IncludeAspect<string>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(cars);
            Assert.IsNull(cars[0].Aspect<string>());
        }

        [Test]
        public async Task IncludeAspect_WhenSameAspectIncludedTwice_ThenProviderQueriedOnce()
        {
            var searcher = new Mock<IEntitySearcher<WildcardQuery, Car>>();
            searcher
                .Setup(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] { new Car("car-1", new CarLocator()) });

            var stringProvider = new Mock<IEntityAspectProvider<CarLocator, string>>();
            var context = new EntityContext.Builder()
                .AddSearcher(searcher.Object)
                .AddAspectProvider(stringProvider.Object)
                .Build();

            var cars = await context.Entities<Car>()
                .List()
                .IncludeAspect<string>()
                .IncludeAspect<string>() // Twice.
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(cars);
            searcher.Verify(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None), Times.Once);
            stringProvider.Verify(p => p.QueryAspect(It.IsAny<CarLocator>()), Times.Once);
        }

        [Test]
        public async Task IncludeAspect_WhenAspectsDiffer()
        {
            var searcher = new Mock<IEntitySearcher<WildcardQuery, Car>>();
            searcher
                .Setup(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] { new Car("car-1", new CarLocator()) });

            var stringProvider = new Mock<IEntityAspectProvider<CarLocator, string>>();
            var versionProvider = new Mock<IEntityAspectProvider<CarLocator, Version>>();
            var context = new EntityContext.Builder()
                .AddSearcher(searcher.Object)
                .AddAspectProvider(stringProvider.Object)
                .AddAspectProvider(versionProvider.Object)
                .Build();

            var cars = await context.Entities<Car>()
                .List()
                .IncludeAspect<string>()
                .IncludeAspect<Version>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(cars);
            searcher.Verify(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None), Times.Once);
            stringProvider.Verify(p => p.QueryAspect(It.IsAny<CarLocator>()), Times.Once);
            versionProvider.Verify(p => p.QueryAspect(It.IsAny<CarLocator>()), Times.Once);
        }

        //----------------------------------------------------------------------
        // IncludeAspect - derived.
        //----------------------------------------------------------------------

        [Test]
        public async Task IncludeAspect_Derived()
        {
            var searcher = new Mock<IEntitySearcher<WildcardQuery, Car>>();
            searcher
                .Setup(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] { new Car("car-1", new CarLocator()) });

            var versionProvider = new Mock<IEntityAspectProvider<CarLocator, Version>>();
            versionProvider
                .Setup(p => p.QueryAspect(It.IsAny<CarLocator>()))
                .Returns(new Version(1, 2));

            var context = new EntityContext.Builder()
                .AddSearcher(searcher.Object)
                .AddAspectProvider(versionProvider.Object)
                .Build();

            var cars = await context.Entities<Car>()
                .List()
                .IncludeAspect<Version, string>(v => v?.ToString())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.IsNotEmpty(cars);
            Assert.AreEqual("1.2", cars[0].Aspect<string>());
        }

        [TestFixture]
        public class Builder
        {
            //------------------------------------------------------------------
            // Get.
            //------------------------------------------------------------------

            [Test]
            public async Task Get_WhenAspectNotFound()
            {
                var provider = new Mock<IEntityAspectProvider<CarLocator, Car>>();
                var context = new EntityContext.Builder()
                    .AddAspectProvider(provider.Object)
                    .Build();

                var cars = await context.Entities<Car>()
                    .Get(new CarLocator())
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.IsEmpty(cars);

                provider.Verify(p => p.QueryAspect(It.IsAny<CarLocator>()), Times.Once);
            }

            [Test]
            public async Task Get()
            {
                var locator = new CarLocator();

                var provider = new Mock<IEntityAspectProvider<CarLocator, Car>>();
                provider
                    .Setup(p => p.QueryAspect(locator))
                    .Returns(new Car("car-1", locator));
                var context = new EntityContext.Builder()
                    .AddAspectProvider(provider.Object)
                    .Build();

                var cars = await context.Entities<Car>()
                    .Get(locator)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.IsNotEmpty(cars);
                Assert.AreEqual("car-1", cars[0].Entity.DisplayName);
            }
        }
    }
}
