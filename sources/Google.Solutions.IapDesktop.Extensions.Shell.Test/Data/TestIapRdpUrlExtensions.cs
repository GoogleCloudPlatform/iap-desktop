using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using NUnit.Framework;
using System.Collections.Specialized;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Data
{
    [TestFixture]
    public class TestIapRdpUrlExtensions
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        //---------------------------------------------------------------------
        // ApplyUrlParameterIfSet<TEnum>.
        //---------------------------------------------------------------------

        [Test]
        public void WhenEnumQueryParameterMissing_ThenApplyLeavesOriginalValue()
        {
            var parameters = new RdpSessionParameters(RdpCredentials.Empty);
            parameters.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, new NameValueCollection()),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenEnumQueryParameterIsNullOrEmpty_ThenApplyLeavesOriginalValue(
            [Values(null, "", " ")] string emptyValue)
        {
            var parameters = new RdpSessionParameters(RdpCredentials.Empty);
            parameters.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", emptyValue);

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenEnumQueryParameterOutOfRange_ThenApplyLeavesOriginalValue(
            [Values("-1", "999999999")] string wrongValue)
        {
            var parameters = new RdpSessionParameters(RdpCredentials.Empty);
            parameters.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", wrongValue);

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.PlayOnServer, parameters.AudioMode);
        }

        [Test]
        public void WhenEnumQueryParameterValid_ThenApplyReplacesOriginalValue()
        {
            var parameters = new RdpSessionParameters(RdpCredentials.Empty);
            parameters.AudioMode = RdpAudioMode.PlayOnServer;
            Assert.AreNotEqual(RdpAudioMode._Default, parameters.AudioMode);

            var queryParameters = new NameValueCollection();
            queryParameters.Add("AudioMode", "2");

            parameters.ApplyUrlParameterIfSet<RdpAudioMode>(
                new IapRdpUrl(SampleLocator, queryParameters),
                "AudioMode",
                (p, v) => p.AudioMode = v);

            Assert.AreEqual(RdpAudioMode.DoNotPlay, parameters.AudioMode);
        }
    }
}
