using Google.Solutions.IapDesktop.Application.Adapters;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Adapters
{
    [TestFixture]
    public class TestGithubAdapter
    {
        [Test]
        public async Task WhenFindingLatestRelease_OneReleaseIsReturned()
        {
            var adapter = new GithubAdapter();
            var release = await adapter.FindLatestReleaseAsync(CancellationToken.None);

            Assert.IsNotNull(release);
            Assert.IsTrue(release.TagVersion.Major >= 1);
        }
    }
}
