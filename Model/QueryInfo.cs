namespace SixDegrees.Model
{
    class QueryInfo
    {
        internal static bool UsesMaxID(QueryType type)
        {
            switch (type)
            {
                case QueryType.TweetsByHashtag:
                case QueryType.LocationsByHashtag:
                    return true;
                case QueryType.UserByScreenName:
                case QueryType.UserConnectionsByScreenName:
                    return false;
                default:
                    return false;
            }
        }

        internal string LastQuery { get; set; } = "";
        internal string LastMaxID { get; set; } = "";
        internal RateLimitInfo RateLimitInfo { get; }

        internal QueryInfo(QueryType type)
        {
            RateLimitInfo = new RateLimitInfo(type);
        }
    }
}
