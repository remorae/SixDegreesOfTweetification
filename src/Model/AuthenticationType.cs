namespace SixDegrees.Model
{
    /// <summary>
    /// Used for logic that differs between Twitter application rate limits and user rate limits.
    /// </summary>
    public enum AuthenticationType
    {
        Application,
        User,
        Both
    }
}
