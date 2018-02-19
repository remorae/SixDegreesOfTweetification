using System;

namespace SixDegrees.Model
{
    public class RateLimitInfo
    {
        //TODO: Get and store limits by Twitter API endpoint to support multiple-endpoint operations
        public static bool SupportsAppAuth(QueryType type)
        {
            switch (type)
            {
                default:
                    return true;
            }
        }

        public static bool SupportsUserAuth(QueryType type)
        {
            switch (type)
            {
                default:
                    return false;
            }
        }

        public static (int User, int Application) AuthLimits(QueryType type)
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

        private DateTime lastUpdated = DateTime.Now;

        public int AppAuthRemaining { get; private set; }
        public int UserAuthRemaining { get; private set; }
        public TimeSpan SinceLastUpdate => DateTime.Now - lastUpdated;

        public RateLimitInfo(QueryType type)
        {
            AppAuthRemaining = AuthLimits(type).Application;
            UserAuthRemaining = AuthLimits(type).User;
        }

        public void Update(int appAuthRemaining, int userAuthRemaining)
        {
            AppAuthRemaining = appAuthRemaining;
            UserAuthRemaining = userAuthRemaining;
            lastUpdated = DateTime.Now;
        }
    }
}
