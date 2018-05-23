namespace SixDegrees.Model
{
    /// <summary>
    /// Distinguishes between SixDegrees API calls.
    /// </summary>
    public enum QueryType
    {
        TweetsByHashtag,
        LocationsByHashtag,
        UserByScreenName,
        UserConnectionsByScreenName,
        HashtagsFromHashtag,
        HashtagConnectionsByHashtag,
        UserConnectionsByID
    }
}
