using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Core.EntityModel;
using Google.Solutions.IapDesktop.Core.ResourceModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Core.Test.EntityModel
{
    [TestFixture]
    public class TestEntityContext
    {
        public class SampleLocator : ILocator
        {
            public string ResourceType => "sample";
        }

        public class SampleEntity : IEntity
        {
            public string DisplayName { get; }

            public ILocator Locator { get; }

            public SampleEntity(string displayName, ILocator locator)
            {
                this.DisplayName = displayName;
                this.Locator = locator;
            }
        }

        //--------------------------------------------------------------------
        // Ctor.
        //--------------------------------------------------------------------


        //--------------------------------------------------------------------
        // List.
        //--------------------------------------------------------------------

        [Test]
        public async Task List_WhenLocatorNotRegistered()
        {
            var context = new EntityContext.Builder().Build();
            var entities = await context
                .ListAsync<IEntity>(new SampleLocator(), CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsEmpty(entities);
        }

        [Test]
        public async Task List()
        {
            var locator = new SampleLocator();
            var container = new Mock<IEntityContainer<SampleLocator, SampleEntity>>();
            container
                .Setup(c => c.ListAsync(It.IsIn(locator), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { new SampleEntity("Sample", locator) });

            var workspace = new EntityContext.Builder()
                .AddContainer(container.Object)
                .Build();

            var entities = await workspace
                .ListAsync<IEntity>(locator, CancellationToken.None)
                .ConfigureAwait(false);
            CollectionAssert.IsNotEmpty(entities);
        }
    }
}
