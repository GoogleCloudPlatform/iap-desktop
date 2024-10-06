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
using Google.Solutions.IapDesktop.Core.EntityModel.Introspection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.EntityModel
{
    [TestFixture]
    public class TestEntityContext
    {
        public class CarLocator : ILocator
        {
            public string ResourceType => "car";
        }

        public class BikeLocator : ILocator
        {
            public string ResourceType => "bike";
        }

        public class GarageLocator : ILocator
        {
            public string ResourceType => "garage";
        }

        public interface IVehicle : IEntity { }

        public class Car : IVehicle, IEntity<CarLocator>
        {
            public string DisplayName { get; }

            public CarLocator Locator { get; }

            public Car(string displayName, CarLocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class Bike : IVehicle, IEntity<BikeLocator>
        {
            public string DisplayName { get; }

            public BikeLocator Locator { get; }

            public Bike(string displayName, BikeLocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class ColorAspect { }
        public class ShapeAspect { }

        private class Expander<TLocator, TEntity> 
            : IEntityExpander<TLocator, TEntity>
            where TLocator : ILocator
            where TEntity : IEntity
        {
            private readonly ICollection<TEntity> entities;

            public Expander(ICollection<TEntity> entities)
            {
                this.entities = entities;
            }

            public void Invalidate(TLocator locator)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<TEntity>> ExpandAsync(
                TLocator locator, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IEnumerable<TEntity>>(this.entities);
            }

            public Task<ICollection<TEntity>> SearchAsync(
                string query, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult<ICollection<TEntity>>(this.entities
                    .Where(e => e.DisplayName.Contains(query))
                    .ToList());
            }
        }

        private class Searcher<TEntity> : IEntitySearcher<string, TEntity> 
            where TEntity : IEntity
        {
            private readonly ICollection<TEntity> entities;

            public Searcher(ICollection<TEntity> entities)
            {
                this.entities = entities;
            }

            public Task<IEnumerable<TEntity>> SearchAsync(
                string query,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IEnumerable<TEntity>>(this.entities
                    .Where(e => e.DisplayName.Contains(query))
                    .ToList());
            }
        }

        //--------------------------------------------------------------------
        // SupportsExpansion.
        //--------------------------------------------------------------------

        [Test]
        public void SupportsExpansion_WhenNoExpanderRegistered()
        {
            var context = new EntityContext.Builder().Build();
            Assert.IsFalse(context.SupportsExpansion(new CarLocator()));
            Assert.IsFalse(context.SupportsExpansion<CarLocator>());
            Assert.IsFalse(context.SupportsExpansion(typeof(string)));
        }

        [Test]
        public void SupportsExpansion_WhenExpandersRegistered()
        {
            var context = new EntityContext.Builder()
                .AddExpander(new Mock<IEntityExpander<GarageLocator, Car>>().Object)
                .AddExpander(new Mock<IEntityExpander<GarageLocator, Bike>>().Object)
                .Build();
            Assert.IsTrue(context.SupportsExpansion(new GarageLocator()));
            Assert.IsTrue(context.SupportsExpansion<GarageLocator>());
        }

        //--------------------------------------------------------------------
        // SupportsAspect.
        //--------------------------------------------------------------------

        [Test]
        public void SupportsAspect_WhenNoProviderRegisteredForLocator()
        {
            var context = new EntityContext.Builder().Build();
            Assert.IsFalse(context.SupportsAspect<CarLocator, Car>());
            Assert.IsFalse(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenNoProviderRegisteredForAspect()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, Car>>().Object)
                .Build();
            Assert.IsFalse(context.SupportsAspect<CarLocator, ShapeAspect>());
            Assert.IsFalse(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenProviderRegistered()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, Car>>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build();
            Assert.IsTrue(context.SupportsAspect<CarLocator, Car>());
            Assert.IsTrue(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenAsyncProviderRegistered()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, Car>>().Object)
                .Build();
            Assert.IsTrue(context.SupportsAspect<CarLocator, Car>());
        }

        //--------------------------------------------------------------------
        // Invalidate.
        //--------------------------------------------------------------------

        [Test]
        public void Invalidate_WhenNoCachesRegisteredForLocator()
        {
            var context = new EntityContext.Builder()
                .AddCache(new Mock<IEntityCache>().Object)
                .AddCache(new Mock<IEntityCache<CarLocator>>().Object)
                .Build();

            context.Invalidate(new BikeLocator());
        }

        [Test]
        public void Invalidate_WhenCachesRegisteredForLocator()
        {
            var cache1 = new Mock<IEntityCache<CarLocator>>();
            var cache2 = new Mock<IEntityCache<CarLocator>>();
            var context = new EntityContext.Builder()
                .AddCache(cache1.Object)
                .AddCache(cache2.Object)
                .Build();

            var car = new CarLocator();
            context.Invalidate(car);

            cache1.Verify(c => c.Invalidate(car), Times.Once);
            cache2.Verify(c => c.Invalidate(car), Times.Once);
        }

        //--------------------------------------------------------------------
        // Expand.
        //--------------------------------------------------------------------

        [Test]
        public async Task Expand_WhenNoContainerRegisteredForLocator()
        {
            var context = new EntityContext.Builder()
                .AddExpander(new Mock<IEntityExpander<GarageLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ExpandAsync<IEntity>(new BikeLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Expand_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder()
                .AddExpander(new Mock<IEntityExpander<GarageLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ExpandAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Expand_WhenEntityTypeDoesNotMatch()
        {
            var container = new Expander<GarageLocator, Car>(
                new[] {
                    new Car("c1", new CarLocator()), 
                    new Car("c2", new CarLocator()) 
                });
            var context = new EntityContext.Builder()
                .AddExpander(container)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ExpandAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Expand()
        {
            var carContainer = new Expander<GarageLocator, Car>(
                new[] {
                    new Car("c1", new CarLocator()),
                    new Car("c2", new CarLocator())
                });
            var bikeContainer = new Expander<GarageLocator, Bike>(
                new[] {
                    new Bike("b1", new BikeLocator())
                });
            var context = new EntityContext.Builder()
                .AddExpander(carContainer)
                .AddExpander(bikeContainer)
                .Build();

            Assert.AreEqual(2, (await context
                .ExpandAsync<Car>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(1, (await context
                .ExpandAsync<Bike>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(3, (await context
                .ExpandAsync<IVehicle>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
        }

        //--------------------------------------------------------------------
        // Search.
        //--------------------------------------------------------------------

        [Test]
        public async Task Search_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder()
                .AddSearcher(new Mock<IEntitySearcher<string, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .SearchAsync<string, Bike>("*", CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Search_WhenNoContainerRegisteredForQueryType()
        {
            var context = new EntityContext.Builder()
                .AddSearcher(new Mock<IEntitySearcher<string, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .SearchAsync<int, Car>(0, CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Search_ResultsOrderedByTypeThenName()
        {
            var carSearcher = new Searcher<Car>(
                new[] {
                    new Car("sample-car3", new CarLocator()),
                    new Car("sample-car1", new CarLocator()),
                    new Car("sample-car2", new CarLocator())
                });
            var bikeSearcher = new Searcher<Bike>(
                new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });
            var context = new EntityContext.Builder()
                .AddSearcher(carSearcher)
                .AddSearcher(bikeSearcher)
                .Build();

            var vehicles = (await context
                .SearchAsync<string, IVehicle>("sample", CancellationToken.None)
                .ConfigureAwait(false)).ToList();

            Assert.AreEqual("sample-bike1", vehicles[0].DisplayName);
            Assert.AreEqual("sample-car1", vehicles[1].DisplayName);
            Assert.AreEqual("sample-car2", vehicles[2].DisplayName);
            Assert.AreEqual("sample-car3", vehicles[3].DisplayName);
        }

        [Test]
        public async Task Search_FilteredByType()
        {
            var carSearcher = new Searcher<Car>(
                new[] {
                    new Car("sample-car1", new CarLocator()),
                    new Car("sample-car2", new CarLocator())
                });
            var bikeSearcher = new Searcher<Bike>(
                new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });
            var context = new EntityContext.Builder()
                .AddSearcher(carSearcher)
                .AddSearcher(bikeSearcher)
                .Build();

            Assert.AreEqual(2, (await context
                .SearchAsync<string, Car>("car", CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(1, (await context
                .SearchAsync<string, Bike>("bike", CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(3, (await context
                .SearchAsync<string, IVehicle>("sample", CancellationToken.None)
                .ConfigureAwait(false)).Count);
        }

        //--------------------------------------------------------------------
        // QueryAspect.
        //--------------------------------------------------------------------

        [Test]
        public void QueryAspect_WhenNoProviderRegistered()
        {
            var context = new EntityContext.Builder().Build();

            Assert.IsNull(context.QueryAspect<ColorAspect>(new CarLocator()));
        }

        [Test]
        public void QueryAspect_WhenNoProviderRegisteredForAspectType()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>().Object)
                .Build();

            Assert.IsNull(context.QueryAspect<ColorAspect>(new CarLocator()));
        }

        [Test]
        public void QueryAspect_WhenProviderReturnsNull()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>().Object)
                .Build();
            Assert.IsNull(context.QueryAspect<ShapeAspect>(new CarLocator()));
        }

        [Test]
        public void QueryAspect()
        {
            var provider = new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>();
            provider
                .Setup(p => p.QueryAspect(It.IsAny<CarLocator>()))
                .Returns(new ShapeAspect());

            var context = new EntityContext.Builder()
                .AddAspectProvider(provider.Object)
                .Build();
            Assert.IsNotNull(context.QueryAspect<ShapeAspect>(new CarLocator()));
        }

        //--------------------------------------------------------------------
        // QueryAspectAsync.
        //--------------------------------------------------------------------

        [Test]
        public async Task QueryAspectAsync_WhenNoProviderRegistered()
        {
            var context = new EntityContext.Builder().Build();

            Assert.IsNull(await context
                .QueryAspectAsync<ColorAspect>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task QueryAspectAsync_WhenNoProviderRegisteredForAspectType()
        {
            var context = new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>().Object)
                .Build();

            Assert.IsNull(await context
                .QueryAspectAsync<ColorAspect>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task QueryAspect_WhenSynchronousProviderRegistered()
        {
            var provider = new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>();
            provider
                .Setup(p => p.QueryAspect(It.IsAny<CarLocator>()))
                .Returns(new ShapeAspect());

            var context = new EntityContext.Builder()
                .AddAspectProvider(provider.Object)
                .Build();
            Assert.IsNotNull(await context
                .QueryAspectAsync<ShapeAspect>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task QueryAspect_WhenAsyncProviderRegistered()
        {
            var provider = new Mock<IAsyncEntityAspectProvider<CarLocator, ShapeAspect>>();
            provider
                .Setup(p => p.QueryAspectAsync(It.IsAny<CarLocator>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ShapeAspect());

            var context = new EntityContext.Builder()
                .AddAspectProvider(provider.Object)
                .Build();
            Assert.IsNotNull(await context
                .QueryAspectAsync<ShapeAspect>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }


        //--------------------------------------------------------------------
        // Introspection.
        //--------------------------------------------------------------------

        [Test]
        public async Task Introspect()
        {
            var bikeSearcher = new Searcher<Bike>(
                new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });
            var context = new EntityContext.Builder()
                .AddSearcher(bikeSearcher)
                .Build();

            var typeNames = (await context
                .SearchAsync<AnyQuery, EntityType>(AnyQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false))
                .Select(t => t.DisplayName);

            CollectionAssert.AreEquivalent(
                new[] { "Bike", "EntityType" },
                typeNames);
        }

        [Test]
        public async Task Introspect_WhenEntityTypeDoesNotSupportQuery()
        {
            var bikeSearcher = new Searcher<Bike>(
                new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });
            var context = new EntityContext.Builder()
                .AddSearcher(bikeSearcher)
                .Build();

            var bikeType = (await context
                .SearchAsync<AnyQuery, EntityType>(AnyQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false))
                .First(t => t.Type == typeof(Bike));

            Assert.AreEqual(0, (await context
                .ExpandAsync<IEntity>(bikeType.Locator, CancellationToken.None)
                .ConfigureAwait(false))
                .Count());
        }

        [Test]
        public async Task Introspect_Expand()
        {
            var searcher = new Mock<IEntitySearcher<AnyQuery, Bike>>();
            searcher
                .Setup(s => s.SearchAsync(AnyQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });

            var context = new EntityContext.Builder()
                .AddSearcher(searcher.Object)
                .Build();

            var bikeType = (await context
                .SearchAsync<AnyQuery, EntityType>(AnyQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false))
                .First(t => t.Type == typeof(Bike));

            Assert.AreEqual(1, (await context
                .ExpandAsync<IEntity>(bikeType.Locator, CancellationToken.None)
                .ConfigureAwait(false))
                .Count());
        }

        //--------------------------------------------------------------------
        // Builder.
        //--------------------------------------------------------------------

        [Test]
        public void Builder_WhenMultipleProvidersRegisteredForSameAspect()
        {
            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());

            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder()
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());

            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder()
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());
        }

    }
}
