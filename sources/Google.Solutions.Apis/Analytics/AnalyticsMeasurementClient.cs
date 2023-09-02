using Google.Solutions.Common.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Apis.Analytics
{
    /// <summary>
    /// Client for the Google Analytics Measurement API.
    /// 
    /// For details, see
    /// https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference
    /// https://developer.chrome.com/docs/extensions/mv3/tut_analytics/
    /// </summary>
    public interface IAnalyticsMeasurementClient
    {
        /// <summary>
        /// Collect an event.
        /// </summary>
        /// <param name="eventName">
        ///   Event name. This must not be one of the reserved names defined in
        ///   https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference
        /// </param>
        /// <param name="parameters">Event-specific parameters</param>
        Task CollectEventAsync(
            AnalyticsSession session,
            string eventName,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken);
    }

    public class AnalyticsMeasurementClient : IAnalyticsMeasurementClient
    {
        /// <summary>
        /// API secret for stream.
        /// </summary>
        private readonly string apiSecret;

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Measurement ID for stream.
        /// </summary>
        public string MeasurementId { get; }

        //---------------------------------------------------------------------
        // IAnalyticsMeasurementClient.
        //---------------------------------------------------------------------

        public Task CollectEventAsync(
            AnalyticsSession session,
            string eventName,
            Dictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            session.ExpectNotNull(nameof(session));
            eventName.ExpectNotEmpty(nameof(eventName));

            var request = new MeasurementRequest()
            {
                ClientId = session.ClientId,
                UserProperties = session
                    .UserProperties
                    .EnsureNotNull()
                    .ToDictionary(kvp => kvp.Key, kvp => new PropertySection(kvp.Value)),
                Events = new[]
                {
                    new EventSection()
                    {
                        Name = eventName,
                        Parameters = parameters
                            .EnsureNotNull()
                            .Concat(session.GenerateParameters())
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    }
                }
            };

            var prefix = session.DebugMode ? "debug/" : string.Empty;
            var url = $"https://www.google-analytics.com/{prefix}mp/collect?api_secret={this.apiSecret}&measurement_id={this.MeasurementId}";

            throw new NotImplementedException();

            // check validation if debug is enabled
        }

        //---------------------------------------------------------------------
        // Request/response classes.
        //---------------------------------------------------------------------

        internal class MeasurementRequest
        {
            [JsonProperty("client_id")]
            public string ClientId { get; set; }

            [JsonProperty("user_properties")]
            public IDictionary<string, PropertySection> UserProperties { get; set; }

            [JsonProperty("events")]
            public IList<EventSection> Events { get; set; }
        }

        internal class PropertySection
        {
            [JsonProperty("value")]
            public string Value { get; }

            public PropertySection(string value)
            {
                this.Value = value;
            }
        }

        internal class EventSection
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("params")]
            public IDictionary<string, string> Parameters { get; set; }
        }
    }
}
