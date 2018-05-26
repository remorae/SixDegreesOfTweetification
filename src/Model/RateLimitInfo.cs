using System;
using System.Collections.Generic;

namespace SixDegrees.Model
{
    /// <summary>
    /// Represents cached rate limit information for a specific Twitter API endpoint.
    /// </summary>
    public abstract class RateLimitInfo
    {
        /// <summary>
        /// Twitter API rate limits reset every 15 minutes.
        /// </summary>
        protected const long TwitterAPIResetIntervalMillis = (15 * TimeSpan.TicksPerMinute / TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// Default rate limits for each authentication type.
        /// </summary>
        protected static readonly IDictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>> AuthLimits
            = new Dictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>>()
        {
            { TwitterAPIEndpoint.SearchTweets,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 450 },
                    { AuthenticationType.User, 180 }
                }
            },
            { TwitterAPIEndpoint.UsersShow,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 900 },
                    { AuthenticationType.User, 900 }
                }
            },
            { TwitterAPIEndpoint.UsersLookup,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 300 },
                    { AuthenticationType.User, 900 }
                }
            },
            { TwitterAPIEndpoint.RateLimitStatus,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 180 },
                    { AuthenticationType.User, 180 }
                }
            },
            { TwitterAPIEndpoint.OAuthAuthorize,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 1 },
                    { AuthenticationType.User, 1 }
                }
            },
            { TwitterAPIEndpoint.FollowersIDs,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 15 },
                    { AuthenticationType.User, 15 }
                }
            },
            { TwitterAPIEndpoint.FriendsIDs,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 15 },
                    { AuthenticationType.User, 15 }
                }
            }
        };
        internal static readonly IEnumerable<AuthenticationType> UseableAuthTypes = new AuthenticationType[] { AuthenticationType.Application, AuthenticationType.User };

        private TwitterAPIEndpoint type;
        public TwitterAPIEndpoint Type { get => type; set { type = value; OnTypeChanged(); } }

        internal abstract void OnTypeChanged();

        /// <summary>
        ///  When the cached rate limit was last changed.
        /// </summary>
        public DateTime LastUpdated { get; set; }
        /// <summary>
        /// The current cached limit.
        /// </summary>
        public int Limit { get; set; }
        /// <summary>
        /// Whether the cache is out of date.
        /// </summary>
        protected bool NeedsReset => SinceLastUpdate > UntilReset;
        /// <summary>
        /// How long it has been since the cache was last updated.
        /// </summary>
        internal TimeSpan SinceLastUpdate => DateTime.Now - LastUpdated;
        /// <summary>
        /// The reported time remaining until the rate limit window resets according to Twitter.
        /// </summary>
        public TimeSpan UntilReset { get; set; } = TimeSpan.FromMilliseconds(TwitterAPIResetIntervalMillis);
        /// <summary>
        /// Whether the correspondering Twitter API endpoint's rate limit has been exhausted.
        /// </summary>
        internal bool Available => NeedsReset || Limit > 0;

        internal void ResetIfNeeded()
        {
            if (NeedsReset)
                Reset();
        }

        /// <summary>
        /// Resets the cache for the given endpoint.
        /// </summary>
        protected abstract void Reset();

        internal void Update(int remaining)
        {
            ResetIfNeeded();
            Limit = remaining;
        }

        internal void Update(int remaining, TimeSpan untilReset)
        {
            Limit = remaining;
            UntilReset = untilReset;
            LastUpdated = DateTime.Now;
        }
    }
}