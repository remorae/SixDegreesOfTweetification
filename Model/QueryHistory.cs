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

        private IDictionary<TwitterAPIEndpoint, QueryInfo> history = new Dictionary<TwitterAPIEndpoint, QueryInfo>();

        private QueryHistory()
        {
            foreach (TwitterAPIEndpoint endpoint in Enum.GetValues(typeof(TwitterAPIEndpoint)))
                history.Add(endpoint, new QueryInfo());
        }

        internal QueryInfo this[TwitterAPIEndpoint key]
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
