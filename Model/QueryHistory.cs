using System.Collections.Generic;

namespace SixDegrees.Model
{
    public class QueryHistory
    {
        private static QueryHistory instance;

        public static QueryHistory Get
        {
            get
            {
                if (instance == null)
                    instance = new QueryHistory();
                return instance;
            }
        }

        private IDictionary<QueryType, QueryInfo> history = new Dictionary<QueryType, QueryInfo>();

        public QueryInfo this[QueryType key]
        {
            get
            {
                if (!history.ContainsKey(key))
                    history.Add(key, new QueryInfo(key));
                return history[key];
            }
        }
    }
}
