namespace SixDegrees.Model.JSON
{
    public partial class Status
    {
        internal string URL { get { return $"https://www.twitter.com/{User.ScreenName}/status/{IdStr}"; } }
    }
}
