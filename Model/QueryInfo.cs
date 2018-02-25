namespace SixDegrees.Model
{
    class QueryInfo
    {
        internal static bool UsesMaxID(TwitterAPIEndpoint endpoint)
        {
            switch (endpoint)
            {
                case TwitterAPIEndpoint.SearchTweets:
                    return true;
                default:
                    return false;
            }
        }

        internal string LastQuery { get; set; } = "";
        internal string LastMaxID { get; set; } = "";
    }
}
