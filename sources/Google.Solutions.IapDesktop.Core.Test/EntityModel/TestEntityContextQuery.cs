using Google.Solutions.IapDesktop.Core.EntityModel;
using Google.Solutions.IapDesktop.Core.EntityModel.Introspection;
using Moq;
using NUnit.Framework;


namespace Google.Solutions.IapDesktop.Core.Test.EntityModel
{
    [TestFixture]
    public class TestEntityContextQuery
    {
        [Test]
        public void __()
        {
            var context = new EntityContext.Builder().Build();
            context
                .Entities<EntityType>()
                .Search(AnyQuery.Instance);

        }
    }
}
