namespace SixDegrees.Model.JSON
{
    public partial class Status
    {
        internal string URL { get { return $"https://www.twitter.com/{User.ScreenName}/status/{IdStr}"; } }

        public override bool Equals(object obj)
        {
            var other = obj as Status;
            if (other == null)
                return false;
            return IdStr.Equals(other.IdStr) && User.IdStr.Equals(other.User.IdStr);
        }

        public override int GetHashCode()
        {
            return IdStr.GetHashCode() ^ User.IdStr.GetHashCode();
        }
    }
}
