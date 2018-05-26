namespace SixDegrees.Model
{
    /// <summary>
    /// Holds information of a Twitter user.
    /// </summary>
    public class TwitterUser
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string ScreenName { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public long FollowerCount { get; set; }
        public long FriendCount { get; set; }
        public string CreatedAt { get; set; }
        public string TimeZone { get; set; }
        public bool GeoEnabled { get; set; }
        public bool Verified { get; set; }
        public long StatusCount { get; set; }
        public string Lang { get; set; }
        public string ProfileImage { get; set; }
    }
}
