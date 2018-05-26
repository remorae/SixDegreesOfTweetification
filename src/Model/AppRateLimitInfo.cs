using System;

namespace SixDegrees.Model
{
    /// <summary>
    /// Represents cached application rate limits for the Twitter application used at runtime.
    /// </summary>
    class AppRateLimitInfo : RateLimitInfo
    {
        internal AppRateLimitInfo()
        {

        }

        protected override void Reset()
        {
            Limit = AuthLimits[Type][AuthenticationType.Application];
            UntilReset = TimeSpan.FromMilliseconds((SinceLastUpdate - UntilReset).TotalMilliseconds % TwitterAPIResetIntervalMillis);
            LastUpdated = DateTime.Now;
        }

        internal override void OnTypeChanged()
        {
            Limit = AuthLimits[Type][AuthenticationType.Application];
        }
    }
}
