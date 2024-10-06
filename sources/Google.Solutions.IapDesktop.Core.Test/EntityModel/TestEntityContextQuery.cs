using Google.Solutions.IapDesktop.Core.EntityModel;
using Google.Solutions.IapDesktop.Core.EntityModel.Introspection;
using Moq;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;


namespace Google.Solutions.IapDesktop.Core.Test.EntityModel
{
    [TestFixture]
    public class TestEntityContextQuery
    {
        [Test]
        public async Task __()
        {
            var context = new EntityContext.Builder().Build();
            using (var result = await context
                .Entities<EntityType>()
                .List()
                .IncludeAspect<string>()
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false))
            {
                foreach (var entity in result)
                {
                }
            }
        }
    }
}
