using System;

namespace SixDegrees.Model
{
    /// <summary>
    /// Represents cached user rate limits for the Twitter application used at runtime.
    /// </summary>
    public class UserRateLimitInfo : RateLimitInfo
    {
        public string UserID { get; set; }

        protected override void Reset()
        {
            Limit = AuthLimits[Type][AuthenticationType.User];
            UntilReset = TimeSpan.FromMilliseconds((SinceLastUpdate - UntilReset).TotalMilliseconds % TwitterAPIResetIntervalMillis);
            LastUpdated = DateTime.Now;
        }

        internal override void OnTypeChanged()
        {
            Limit = AuthLimits[Type][AuthenticationType.User];
        }
    }
}
