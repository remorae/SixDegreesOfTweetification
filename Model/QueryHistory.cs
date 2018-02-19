using System.Collections.Generic;

namespace SixDegrees.Model
{
    class QueryHistory
    {
        private static QueryHistory instance;

        internal static QueryHistory Get
        {
            get
            {
                if (instance == null)
                    instance = new QueryHistory();
                return instance;
            }
        }

        private IDictionary<QueryType, QueryInfo> history = new Dictionary<QueryType, QueryInfo>();

        private QueryHistory()
        {
        }

        internal QueryInfo this[QueryType key]
        {
            get
            {
                if (!history.ContainsKey(key))
                    history.Add(key, new QueryInfo(key));
                return history[key];
            }
        }

        internal IDictionary<QueryType, IDictionary<AuthenticationType, int>> RateLimits
        {
            get
            {
                var result = new Dictionary<QueryType, IDictionary<AuthenticationType, int>>();
                foreach (var queryPair in history)
                    result.Add(queryPair.Key, queryPair.Value.RateLimitInfo.ToDictionary());
                return result;
            }
        }
    }
}
