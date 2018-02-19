using System;
using System.Collections.Generic;

namespace SixDegrees.Model
{
    class RateLimitInfo
    {
        //TODO: Get and store limits by Twitter API endpoint to support multiple-endpoint operations
        internal static bool SupportsAppAuth(QueryType type)
        {
            switch (type)
            {
                default:
                    return true;
            }
        }

        internal static bool SupportsUserAuth(QueryType type)
        {
            switch (type)
            {
                default:
                    return false;
            }
        }

        internal static (int User, int Application) AuthLimits(QueryType type)
        {
            switch (type)
            {
                case QueryType.TweetsByHashtag:
                case QueryType.LocationsByHashtag:
                    return (180, 450);
                case QueryType.UserByScreenName:
                    return (900, 900);
                case QueryType.UserConnectionsByScreenName:
                    return (900, 300);
                default:
                    return (0, 0);
            }
        }

        private readonly QueryType type;
        private DateTime lastUpdated = DateTime.Now;
        private int appAuthRemaining;
        private int userAuthRemaining;

        internal TimeSpan SinceLastUpdate => DateTime.Now - lastUpdated;
        internal TimeSpan UntilReset { get; private set; } = new TimeSpan(0, 15, 0);

        internal RateLimitInfo(QueryType type)
        {
            this.type = type;
            Reset();
        }

        internal void Reset()
        {
            appAuthRemaining = AuthLimits(type).Application;
            userAuthRemaining = AuthLimits(type).User;
        }

        internal void Update(int appAuthRemaining, int userAuthRemaining)
        {
            this.appAuthRemaining = appAuthRemaining;
            this.userAuthRemaining = userAuthRemaining;
            lastUpdated = DateTime.Now;
        }

        internal void Update(AuthenticationType authType, int remaining, TimeSpan untilReset)
        {
            if (authType == AuthenticationType.Application)
                appAuthRemaining = remaining;
            else
                userAuthRemaining = remaining;
            lastUpdated = DateTime.Now;
        }

        internal IDictionary<AuthenticationType, int> ToDictionary()
        {
            return new Dictionary<AuthenticationType, int>()
            {
                { AuthenticationType.Application, appAuthRemaining },
                { AuthenticationType.User, userAuthRemaining }
            };
        }
    }
}
