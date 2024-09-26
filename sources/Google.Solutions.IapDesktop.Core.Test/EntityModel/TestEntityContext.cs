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

        public class VehicleLocator : ILocator
        {
            public string ResourceType => "vehicle";
        }

        public interface IVehicle : IEntity { }

        public class Car : IVehicle
        {
            public string DisplayName { get; }

            public ILocator Locator { get; }

            public Car(string displayName, ILocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class Bike : IVehicle
        {
            public string DisplayName { get; }

            public ILocator Locator { get; }

            public Bike(string displayName, ILocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        public class ColorAspect { }
        public class ShapeAspect { }

        private class Container<TLocator, TEntity> 
            : IEntityContainer<TLocator, TEntity>, IEntitySearcher<TLocator, TEntity>
            where TLocator : ILocator
            where TEntity : IEntity
        {
            private readonly ICollection<TEntity> entities;

            public Container(ICollection<TEntity> entities)
            {
                this.entities = entities;
            }

            public void Invalidate(TLocator locator)
            {
                throw new NotImplementedException();
            }

            public Task<ICollection<TEntity>> ListAsync(
                TLocator locator, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult(this.entities);
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

        //--------------------------------------------------------------------
        // IsContainer.
        //--------------------------------------------------------------------

        [Test]
        public void IsContainer_WhenNoContainerRegistered()
        {
            var context = new EntityContext.Builder().Build();
            Assert.IsFalse(context.IsContainer(new CarLocator()));
            Assert.IsFalse(context.IsContainer<CarLocator>());
            Assert.IsFalse(context.IsContainer(typeof(string)));
        }

        [Test]
        public void IsContainer_WhenContainersRegistered()
        {
            var context = new EntityContext.Builder()
                .AddContainer(new Mock<IEntityContainer<CarLocator, Car>>().Object)
                .AddContainer(new Mock<IEntityContainer<BikeLocator, Bike>>().Object)
                .Build();
            Assert.IsTrue(context.IsContainer(new CarLocator()));
            Assert.IsTrue(context.IsContainer<CarLocator>());
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
        // List.
        //--------------------------------------------------------------------

        [Test]
        public async Task List_WhenNoContainerRegisteredForLocator()
        {
            var context = new EntityContext.Builder()
                .AddContainer(new Mock<IEntityContainer<CarLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListAsync<IEntity>(new BikeLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task List_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder()
                .AddContainer(new Mock<IEntityContainer<CarLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task List_WhenEntityTypeDoesNotMatch()
        {
            var container = new Container<CarLocator, Car>(
                new[] {
                    new Car("c1", new CarLocator()), 
                    new Car("c2", new CarLocator()) 
                });
            var context = new EntityContext.Builder()
                .AddContainer(container)
                .Build();

            CollectionAssert.IsEmpty(await context
                .ListAsync<Bike>(new CarLocator(), CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task List()
        {
            var carContainer = new Container<VehicleLocator, Car>(
                new[] {
                    new Car("c1", new VehicleLocator()),
                    new Car("c2", new VehicleLocator())
                });
            var bikeContainer = new Container<VehicleLocator, Bike>(
                new[] {
                    new Bike("b1", new VehicleLocator())
                });
            var context = new EntityContext.Builder()
                .AddContainer(carContainer)
                .AddContainer(bikeContainer)
                .Build();

            Assert.AreEqual(2, (await context
                .ListAsync<Car>(new VehicleLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(1, (await context
                .ListAsync<Bike>(new VehicleLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(3, (await context
                .ListAsync<IVehicle>(new VehicleLocator(), CancellationToken.None)
                .ConfigureAwait(false)).Count);
        }

        //--------------------------------------------------------------------
        // Search.
        //--------------------------------------------------------------------

        [Test]
        public async Task Search_WhenNoContainerRegisteredForEntityType()
        {
            var context = new EntityContext.Builder()
                .AddSearcher(new Mock<IEntitySearcher<CarLocator, Car>>().Object)
                .Build();

            CollectionAssert.IsEmpty(await context
                .SearchAsync<Bike>("*", CancellationToken.None)
                .ConfigureAwait(false));
        }

        [Test]
        public async Task Search()
        {
            var carContainer = new Container<VehicleLocator, Car>(
                new[] {
                    new Car("sample-car1", new VehicleLocator()),
                    new Car("sample-car2", new VehicleLocator())
                });
            var bikeContainer = new Container<VehicleLocator, Bike>(
                new[] {
                    new Bike("sample-bike1", new VehicleLocator())
                });
            var context = new EntityContext.Builder()
                .AddSearcher(carContainer)
                .AddSearcher(bikeContainer)
                .Build();

            Assert.AreEqual(2, (await context
                .SearchAsync<Car>("car", CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(1, (await context
                .SearchAsync<Bike>("bike", CancellationToken.None)
                .ConfigureAwait(false)).Count);
            Assert.AreEqual(3, (await context
                .SearchAsync<IVehicle>("sample", CancellationToken.None)
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
