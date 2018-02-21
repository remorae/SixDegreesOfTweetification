using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Model
{
    class RateLimitInfo
    {
        private static readonly IDictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>> AuthLimits
            = new Dictionary<TwitterAPIEndpoint, IDictionary<AuthenticationType, int>>()
        {
            { TwitterAPIEndpoint.SearchTweets,
                new Dictionary<AuthenticationType, int>()
                {
                    { AuthenticationType.Application, 180 },
                    { AuthenticationType.User, 450 }
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
                    { AuthenticationType.Application, 900 },
                    { AuthenticationType.User, 900 }
                }
            }
        };
        
        internal static bool SupportsAppAuth(TwitterAPIEndpoint type)
        {
            switch (type)
            {
                default:
                    return true;
            }
        }

        internal static bool SupportsUserAuth(TwitterAPIEndpoint type)
        {
            switch (type)
            {
                default:
                    return false;
            }
        }

        private readonly TwitterAPIEndpoint type;
        private DateTime lastUpdated = DateTime.Now;
        private IDictionary<AuthenticationType, int> currentLimits = new Dictionary<AuthenticationType, int>();

        internal int this[AuthenticationType type]
        {
            get
            {
                if (!currentLimits.ContainsKey(type))
                    currentLimits.Add(type, AuthLimits[this.type][type]);
                return currentLimits[type];
            }
        }

        internal TimeSpan SinceLastUpdate => DateTime.Now - lastUpdated;
        internal TimeSpan UntilReset { get; private set; } = new TimeSpan(0, 15, 0);

        internal RateLimitInfo(TwitterAPIEndpoint type)
        {
            this.type = type;
            currentLimits.Add(AuthenticationType.Application, AuthLimits[type][AuthenticationType.Application]);
            currentLimits.Add(AuthenticationType.User, AuthLimits[type][AuthenticationType.User]);
        }

        internal void Reset()
        {
            currentLimits[AuthenticationType.Application] = AuthLimits[type][AuthenticationType.Application];
            currentLimits[AuthenticationType.User] = AuthLimits[type][AuthenticationType.User];
        }

        internal void Update(int appAuthRemaining, int userAuthRemaining)
        {
            currentLimits[AuthenticationType.Application] = appAuthRemaining;
            currentLimits[AuthenticationType.User] = userAuthRemaining;
            lastUpdated = DateTime.Now;
        }

        internal void Update(AuthenticationType authType, int remaining, TimeSpan untilReset)
        {
            currentLimits[authType] = remaining;
            lastUpdated = DateTime.Now;
        }
    }
}
