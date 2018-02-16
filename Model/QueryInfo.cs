namespace SixDegrees.Model
{
    public struct QueryInfo
    {
        public string LastQuery;
        public string LastMaxID;

        public QueryInfo(string lastQuery, string lastMaxID)
        {
            LastQuery = lastQuery;
            LastMaxID = lastMaxID;
        }
    }
}
