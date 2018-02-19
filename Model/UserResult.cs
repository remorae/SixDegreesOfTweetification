namespace SixDegrees.Model
{
    public class UserResult
    {
        internal string ID { get; set; }
        internal string Name { get; set; }
        internal string ScreenName { get; set; }
        internal string Location { get; set; }
        internal string Description { get; set; }
        internal long FollowerCount { get; set; }
        internal long FriendCount { get; set; }
        internal string CreatedAt { get; set; }
        internal string TimeZone { get; set; }
        internal bool GeoEnabled { get; set; }
        internal bool Verified { get; set; }
        internal long StatusCount { get; set; }
        internal string Lang { get; set; }
        internal string ProfileImage { get; set; }
    }
}
