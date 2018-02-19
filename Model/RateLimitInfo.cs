using System;

namespace SixDegrees.Model
{
    public class RateLimitInfo
    {

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

        public static int AppAuthLimit(QueryType type)
        {
            if (!SupportsAppAuth(type))
                return 0;

            switch (type)
            {
                default:
                    return 0;
            }
        }

        public static int UserAuthLimit(QueryType type)
        {
            if (!SupportsUserAuth(type))
                return 0;

            switch (type)
            {
                default:
                    return 0;
            }
        }

        private DateTime? lastUpdated = null;

        public int AppAuthRemaining { get; private set; }
        public int UserAuthRemaining { get; private set; }
        public TimeSpan SinceLastUpdate => (lastUpdated.HasValue) ? DateTime.Now - lastUpdated.Value : TimeSpan.MaxValue;

        public RateLimitInfo(QueryType type)
        {
            AppAuthRemaining = AppAuthLimit(type);
            UserAuthRemaining = UserAuthLimit(type);
        }

        public void Update(int appAuthRemaining, int userAuthRemaining)
        {
            AppAuthRemaining = appAuthRemaining;
            UserAuthRemaining = userAuthRemaining;
            lastUpdated = DateTime.Now;
        }
    }
}
