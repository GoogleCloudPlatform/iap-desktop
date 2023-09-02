using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Google.Solutions.Apis.Analytics
{
    /// <summary>
    /// A Google analytics session. 
    /// </summary>
    public class AnalyticsSession
    {
        private long lastEventMsec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// Unique session Id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Client ID, uniquely identifies this installation.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Custom properties about the user, optional.
        /// </summary>
        public IDictionary<string, string> UserProperties { get; }

        /// <summary>
        /// Enable or disable debug mode.
        /// </summary>
        public bool DebugMode { get; set; } = false;

        internal IEnumerable<KeyValuePair<string, string>> GenerateParameters()
        {
            //
            // Calculate time (in milliseconds) since the last
            // event was sent. This time is counted as "engagement time".
            //
            // For details about session tracking, see
            // https://developers.google.com/analytics/devguides/collection/protocol/ga4/sending-events.
            //
            var nowMsec = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timeSinceLastEventMsec =
                nowMsec - Interlocked.Exchange(ref this.lastEventMsec, nowMsec);

            yield return new KeyValuePair<string, string>(
                "engagement_time_msec", 
                timeSinceLastEventMsec.ToString());

            yield return new KeyValuePair<string, string>("session_id", this.Id.ToString());

            if (this.DebugMode)
            {
                yield return new KeyValuePair<string, string>("debug_mode", "true");
            }
        }

        public AnalyticsSession(
            string clientId,
            IDictionary<string, string> userProperties)
        {
            this.Id = Guid.NewGuid();
            this.ClientId = clientId.ExpectNotEmpty(nameof(clientId));
            this.UserProperties = userProperties;
        }
    }
}
