using System;
using System.Collections.Generic;

namespace SixDegrees.Model
{
    public abstract class RateLimitInfo
    {

        protected const long TwitterAPIResetIntervalMillis = (15 * TimeSpan.TicksPerMinute / TimeSpan.TicksPerMillisecond);

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

        public DateTime LastUpdated { get; set; }

        public int Limit { get; set; }
        protected bool NeedsReset => SinceLastUpdate > UntilReset;
        internal TimeSpan SinceLastUpdate => DateTime.Now - LastUpdated;
        public TimeSpan UntilReset { get; set; } = TimeSpan.FromMilliseconds(TwitterAPIResetIntervalMillis);
        internal bool Available => NeedsReset || Limit > 0;

        internal void ResetIfNeeded()
        {
            if (NeedsReset)
                Reset();
        }

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