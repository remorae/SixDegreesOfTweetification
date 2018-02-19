namespace SixDegrees.Model
{
    public class QueryInfo
    {
        public static bool UsesMaxID(QueryType type)
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

        public string LastQuery { get; set; } = "";
        public string LastMaxID { get; set; } = "";
        public RateLimitInfo RateLimitInfo { get; }

        public QueryInfo(QueryType type)
        {
            RateLimitInfo = new RateLimitInfo(type);
        }
    }
}
