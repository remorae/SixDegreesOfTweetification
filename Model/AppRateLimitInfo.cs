using System;

namespace SixDegrees.Model
{
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
