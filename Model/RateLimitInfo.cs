using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Model
{
    class RateLimitInfo
    {
        private const long TwitterAPIResetIntervalMillis = (15 * TimeSpan.TicksPerMinute / TimeSpan.TicksPerMillisecond);
        private static readonly IDictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>> AuthLimits
            = new Dictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>>()
        {
            { TwitterAPIEndpoint.SearchTweets,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 450 },
                    { AuthenticationType.User, 180 }
                }
            },
            { TwitterAPIEndpoint.UserShow,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 900 },
                    { AuthenticationType.User, 900 }
                }
            },
            { TwitterAPIEndpoint.UserLookup,
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
            }
        };

        internal static bool SupportsAppAuth(TwitterAPIEndpoint type) => AuthLimits[type][AuthenticationType.Application] > 0;
        internal static bool SupportsUserAuth(TwitterAPIEndpoint type) => AuthLimits[type][AuthenticationType.User] > 0;

        private readonly TwitterAPIEndpoint type;
        private DateTime lastUpdated = DateTime.Now;
        private IDictionary<AuthenticationType, int> currentLimits = new Dictionary<AuthenticationType, int>();

        internal int this[AuthenticationType type] => currentLimits[type];

        internal bool NeedsReset => SinceLastUpdate > UntilReset;
        internal TimeSpan SinceLastUpdate => DateTime.Now - lastUpdated;
        internal TimeSpan UntilReset { get; private set; } = TimeSpan.FromMilliseconds(TwitterAPIResetIntervalMillis);
        internal bool Available => NeedsReset || Enum.GetValues(typeof(AuthenticationType)).Cast<AuthenticationType>().Any(authType => currentLimits[authType] > 0);

        internal RateLimitInfo(TwitterAPIEndpoint type)
        {
            this.type = type;
            foreach (AuthenticationType authType in Enum.GetValues(typeof(AuthenticationType)))
                currentLimits.Add(authType, AuthLimits[type][authType]);
        }

        internal void ResetIfNeeded()
        {
            if (NeedsReset)
                Reset();
        }

        private void Reset()
        {
            foreach (AuthenticationType authType in Enum.GetValues(typeof(AuthenticationType)))
                currentLimits[authType] = AuthLimits[type][authType];
            UntilReset = TimeSpan.FromMilliseconds((SinceLastUpdate - UntilReset).TotalMilliseconds % TwitterAPIResetIntervalMillis);
            lastUpdated = DateTime.Now;
        }

        internal void Update(int appAuthRemaining, int userAuthRemaining)
        {
            ResetIfNeeded();
            currentLimits[AuthenticationType.Application] = appAuthRemaining;
            currentLimits[AuthenticationType.User] = userAuthRemaining;
        }

        internal void Update(AuthenticationType authType, int remaining, TimeSpan untilReset)
        {
            currentLimits[authType] = remaining;
            UntilReset = untilReset;
            lastUpdated = DateTime.Now;
        }
    }
}
