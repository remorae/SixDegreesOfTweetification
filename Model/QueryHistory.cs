using System;
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
            foreach (QueryType type in Enum.GetValues(typeof(QueryType)))
                history.Add(type, new QueryInfo());
        }

        internal QueryInfo this[QueryType key]
        {
            get
            {
                if (!history.ContainsKey(key))
                    history.Add(key, new QueryInfo());
                return history[key];
            }
        }
    }
}
