using Google.Apis.Util;
using Google.Solutions.Apis.Auth;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Host.Adapters;
using System;


namespace Google.Solutions.IapDesktop.Application.Host
{
    /// <summary>
    /// Policy that determines how often to check for updates,
    /// and which updates to apply.
    /// </summary>
    internal class UpdatePolicy
    {
        /// <summary>
        /// Determines how often update checks are performed. 
        /// A higher number implies a slower pace of updates.
        /// </summary>
        public const int DaysBetweenUpdateChecks = 10;

        private readonly IClock clock;

        public UpdatePolicy(
            IAuthorization authorization,
            IClock clock)
        {
            authorization.ExpectNotNull(nameof(authorization));

            this.clock = clock.ExpectNotNull(nameof(clock));

            //
            // Only install critical and normal updates by default.
            //
            this.FollowedTracks = ReleaseTrack.Critical | ReleaseTrack.Normal;

            if (authorization.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith("@google.com", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith(".joonix.net", StringComparison.OrdinalIgnoreCase) ||
                authorization.Email.EndsWith(".altostrat.com", StringComparison.OrdinalIgnoreCase))
            {
                //
                // Opt-in these domains to the rapid track.
                //

                this.FollowedTracks |= ReleaseTrack.Rapid;
            }
        }

        /// <summary>
        /// Release track followed by this user/machine.
        /// </summary>
        public ReleaseTrack FollowedTracks { get; }

        /// <summary>
        /// Determine which release track a release belongs to.
        /// </summary>
        public ReleaseTrack GetReleaseTrack(IGitHubRelease release)
        {
            //
            // GitHub doesn't let us "tag" releases in a good way,
            // so we look for a tag in the description text.
            //
            var description = release.Description ?? string.Empty;

            if (description.Contains("[track:critical]"))
            {
                return ReleaseTrack.Critical;
            }
            else if (description.Contains("[track:rapid]"))
            {
                return ReleaseTrack.Rapid;
            }
            else if (description.Contains("[track:optional]"))
            {
                return ReleaseTrack.Optional;
            }
            else
            {
                return ReleaseTrack.Normal;
            }
        }

        /// <summary>
        /// Determine if the user should be advised to install an update.
        /// </summary>
        public bool IsUpdateAdvised(IGitHubRelease release) //TODO: Rename to IRelease
        {
            Precondition.ExpectNotNull(release, nameof(release));

            return this.FollowedTracks.HasFlag(GetReleaseTrack(release));
        }

        /// <summary>
        /// Determine if an update check should be performed.
        /// </summary>
        public bool IsUpdateCheckDue(DateTime lastCheck)
        {
            return (this.clock.UtcNow - lastCheck).TotalDays >= DaysBetweenUpdateChecks;
        }
    }

    /// <summary>
    /// Available release tracks.
    /// </summary>
    [Flags]
    internal enum ReleaseTrack : uint //TODO: Move to separate file.
    {
        /// <summary>
        /// Critical security updates.
        /// </summary>
        Critical = 1,

        /// <summary>
        /// Normal feature updates (later stage).
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Normal feature updates (early stage).
        /// </summary>
        Rapid = 4,

        /// <summary>
        /// Optional feature updates that might only be relevant to few.
        /// </summary>
        Optional = 8,
    }
}
