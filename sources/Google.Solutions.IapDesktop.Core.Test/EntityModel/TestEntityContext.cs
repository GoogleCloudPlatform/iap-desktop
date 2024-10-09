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
using Google.Solutions.IapDesktop.Core.ObjectModel;
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
        public abstract class VehicleLocator : ILocator
        {
            public abstract string ResourceType { get; }
        }

        public class CarLocator : VehicleLocator
        {
            public override string ResourceType => "car";
        }

        public class BikeLocator : VehicleLocator
        {
            public override string ResourceType => "bike";
        }

        public class GarageLocator : VehicleLocator
        {
            public override string ResourceType => "garage";
        }

        public interface IVehicle : IEntity<VehicleLocator> { }

        public class Car : IVehicle, IEntity<VehicleLocator>
        {
            public string DisplayName { get; }

            public VehicleLocator Locator { get; }

            public Car(string displayName, CarLocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class Bike : IVehicle, IEntity<VehicleLocator>
        {
            public string DisplayName { get; }

            public VehicleLocator Locator { get; }

            public Bike(string displayName, BikeLocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class ColorAspect { }
        public class ShapeAspect { }

        private class Navigator<TLocator, TEntity> 
            : IEntityNavigator<TLocator, TEntity>
            where TLocator : ILocator
            where TEntity : IEntity<ILocator>
        {
            private readonly ICollection<TEntity> entities;

            public Navigator(ICollection<TEntity> entities)
            {
                this.entities = entities;
            }

            public void Invalidate(TLocator locator)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<TEntity>> ListDescendantsAsync(
                TLocator locator, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IEnumerable<TEntity>>(this.entities);
            }
        }

        private class Searcher<TEntity> : IEntitySearcher<string, TEntity> 
            where TEntity : IEntity<ILocator>
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
        public void SupportsExpansion_WhenNoNavigatorRegistered()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object).Build();
            Assert.IsFalse(context.SupportsDescendants(new CarLocator()));
            Assert.IsFalse(context.SupportsDescendants<CarLocator>());
            Assert.IsFalse(context.SupportsDescendants(typeof(string)));
        }

        [Test]
        public void SupportsExpansion_WhenNavigatorRegistered()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddNavigator(new Mock<IEntityNavigator<GarageLocator, Car>>().Object)
                .AddNavigator(new Mock<IEntityNavigator<GarageLocator, Bike>>().Object)
                .Build();
            Assert.IsTrue(context.SupportsDescendants(new GarageLocator()));
            Assert.IsTrue(context.SupportsDescendants<GarageLocator>());
        }

        //--------------------------------------------------------------------
        // SupportsAspect.
        //--------------------------------------------------------------------

        [Test]
        public void SupportsAspect_WhenNoProviderRegisteredForLocator()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object).Build();
            Assert.IsFalse(context.SupportsAspect<CarLocator, Car>());
            Assert.IsFalse(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenNoProviderRegisteredForAspect()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, Car>>().Object)
                .Build();
            Assert.IsFalse(context.SupportsAspect<CarLocator, ShapeAspect>());
            Assert.IsFalse(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenProviderRegistered()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, Car>>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build();
            Assert.IsTrue(context.SupportsAspect<CarLocator, Car>());
            Assert.IsTrue(context.SupportsAspect<CarLocator, ColorAspect>());
        }

        [Test]
        public void SupportsAspect_WhenAsyncProviderRegistered()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddCache(cache1.Object)
                .AddCache(cache2.Object)
                .Build();

            var car = new CarLocator();
            context.Invalidate(car);

            cache1.Verify(c => c.Invalidate(car), Times.Once);
            cache2.Verify(c => c.Invalidate(car), Times.Once);
        }

        //--------------------------------------------------------------------
        // ListDescendant.
        //--------------------------------------------------------------------

        [Test]
        public async Task ListDescendants_WhenNoContainerRegisteredForLocator()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddNavigator(new Mock<IEntityNavigator<GarageLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListDescendantsAsync<IEntity<ILocator>>(new BikeLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task ListDescendants_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddNavigator(new Mock<IEntityNavigator<GarageLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListDescendantsAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task ListDescendants_WhenEntityTypeDoesNotMatch()
        {
            var container = new Navigator<GarageLocator, Car>(
                new[] {
                    new Car("c1", new CarLocator()), 
                    new Car("c2", new CarLocator()) 
                });
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddNavigator(container)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListDescendantsAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task ListDescendants()
        {
            var carContainer = new Navigator<GarageLocator, Car>(
                new[] {
                    new Car("c1", new CarLocator()),
                    new Car("c2", new CarLocator())
                });
            var bikeContainer = new Navigator<GarageLocator, Bike>(
                new[] {
                    new Bike("b1", new BikeLocator())
                });
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddNavigator(carContainer)
                .AddNavigator(bikeContainer)
                .Build();

            Assert.AreEqual(2, (await context
                .ListDescendantsAsync<Car>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(1, (await context
                .ListDescendantsAsync<Bike>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(3, (await context
                .ListDescendantsAsync<IVehicle>(new GarageLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
        }

        //--------------------------------------------------------------------
        // Search.
        //--------------------------------------------------------------------

        [Test]
        public async Task Search_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddSearcher(new Mock<IEntitySearcher<string, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .SearchAsync<string, Bike>("*", CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Search_WhenNoContainerRegisteredForQueryType()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object).Build();

            Assert.IsNull(context.QueryAspect<ColorAspect>(new CarLocator()));
        }

        [Test]
        public void QueryAspect_WhenNoProviderRegisteredForAspectType()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ShapeAspect>>().Object)
                .Build();

            Assert.IsNull(context.QueryAspect<ColorAspect>(new CarLocator()));
        }

        [Test]
        public void QueryAspect_WhenProviderReturnsNull()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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

            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object).Build();

            Assert.IsNull(await context
                .QueryAspectAsync<ColorAspect>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task QueryAspectAsync_WhenNoProviderRegisteredForAspectType()
        {
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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

            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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

            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddSearcher(bikeSearcher)
                .Build();

            var typeNames = (await context
                .SearchAsync<WildcardQuery, EntityType>(WildcardQuery.Instance, CancellationToken.None)
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
            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddSearcher(bikeSearcher)
                .Build();

            var bikeType = (await context
                .SearchAsync<WildcardQuery, EntityType>(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false))
                .First(t => t.Type == typeof(Bike));

            Assert.AreEqual(0, (await context
                .ListDescendantsAsync<IEntity<ILocator>>(bikeType.Locator, CancellationToken.None)
                .ConfigureAwait(false))
                .Count);
        }

        [Test]
        public async Task Introspect_ListDescendants()
        {
            var searcher = new Mock<IEntitySearcher<WildcardQuery, Bike>>();
            searcher
                .Setup(s => s.SearchAsync(WildcardQuery.Instance, CancellationToken.None))
                .ReturnsAsync(new[] {
                    new Bike("sample-bike1", new BikeLocator())
                });

            var context = new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddSearcher(searcher.Object)
                .Build();

            var bikeType = (await context
                .SearchAsync<WildcardQuery, EntityType>(WildcardQuery.Instance, CancellationToken.None)
                .ConfigureAwait(false))
                .First(t => t.Type == typeof(Bike));

            Assert.AreEqual(1, (await context
                .ListDescendantsAsync<IEntity<ILocator>>(bikeType.Locator, CancellationToken.None)
                .ConfigureAwait(false))
                .Count);
        }

        //--------------------------------------------------------------------
        // Builder.
        //--------------------------------------------------------------------

        [Test]
        public void Builder_WhenMultipleProvidersRegisteredForSameAspect()
        {
            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());

            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());

            Assert.Throws<InvalidOperationException>(
                () => new EntityContext.Builder(new Mock<IEventQueue>().Object)
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .AddAspectProvider(new Mock<IAsyncEntityAspectProvider<CarLocator, ColorAspect>>().Object)
                .Build());
        }

    }
}
