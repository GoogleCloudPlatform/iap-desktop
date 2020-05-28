using Google.Apis.Compute.v1;
using Google.Apis.Services;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test.Testbed;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Test.History
{

    [TestFixture]
    [Category("IntegrationTest")]
    public class TestLicenseLoader : FixtureBase
    {
        private AnnotatedInstanceSetHistory CreateSet(ImageLocator image)
        {
            return new AnnotatedInstanceSetHistory(
                new InstanceSetHistory(
                    new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new[]
                    {
                        new InstanceHistory(
                            188550847350222232,
                            reference: new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                            InstanceHistoryState.Complete,
                            image,
                            Enumerable.Empty<InstancePlacement>())
                    }));
        }

        [Test]
        public async Task WhenImageFound_ThenAnnotationIsAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("windows-cloud", "family/windows-2019"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeService.Images);

            Assert.AreEqual(1, annotatedSet.LicenseAnnotations.Count());

            var annotation = annotatedSet.LicenseAnnotations.Values.First();
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);
        }

        [Test]
        public async Task WhenImageNotFoundButFromWindowsProject_ThenAnnotationIsAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("windows-cloud", "windows-95"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeService.Images);

            Assert.AreEqual(1, annotatedSet.LicenseAnnotations.Count());

            var annotation = annotatedSet.LicenseAnnotations.Values.First();
            Assert.AreEqual(OperatingSystemTypes.Windows, annotation.OperatingSystem);
            Assert.AreEqual(LicenseTypes.Spla, annotation.LicenseType);
        }

        [Test]
        public async Task WhenImageNotFound_ThenAnnotationNotAdded()
        {
            var annotatedSet = CreateSet(
                new ImageLocator("unknown", "beos"));

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());

            var computeService = new ComputeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Defaults.GetCredential()
            });

            await LicenseLoader.LoadLicenseAnnotationsAsync(
                annotatedSet,
                computeService.Images);

            Assert.AreEqual(0, annotatedSet.LicenseAnnotations.Count());
        }
    }
}
