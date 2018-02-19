using SixDegrees.Model.JSON;
using System;

namespace SixDegrees.Model
{
    public enum QueryType
    {
        TweetsByHashtag,
        LocationsByHashtag,
        UserByScreenName,
        UserConnectionsByScreenName
    }
}
