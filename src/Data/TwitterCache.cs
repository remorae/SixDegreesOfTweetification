using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver.V1;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Data
{
    /// <summary>
    /// Handles interactions with the Neo4j database when retrieving Twitter information.
    /// </summary>
    static class TwitterCache
    {
        private static IDriver GetDriver(IConfiguration configuration) =>
            GraphDatabase.Driver(configuration["twitterCacheURI"],
                AuthTokens.Basic(configuration["twitterCacheUser"], configuration["twitterCachePassword"]));

        /// <summary>
        /// Ensures all given users are in the cache.
        /// </summary>
        /// <seealso cref="UpdateUsersByIDs(IConfiguration, IEnumerable{string})"/>
        /// <param name="configuration"></param>
        /// <param name="users"></param>
        internal static void UpdateUsers(IConfiguration configuration, IEnumerable<TwitterUser> users)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                ICollection<TwitterUser> toStore = new List<TwitterUser>();
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var user in users.Except(toStore))
                        session.WriteTransaction(tx => UpdateUser(tx, user));
                }
            }
        }

        private static void UpdateUser(ITransaction tx, TwitterUser user)
        {
            tx.Run("MERGE (user:User {id: $ID}) " +
                "SET user.name = $Name, user.screenName = $ScreenName, user.location = $Location, " +
                "user.description = $Description, user.followerCount = $FollowerCount, user.friendCount = $FriendCount, " +
                "user.createdAt = $CreatedAt, user.timeZone = $TimeZone, user.geoEnabled = $GeoEnabled, " +
                "user.verified = $Verified, user.statusCount = $StatusCount, user.lang = $Lang, user.profileImage = $ProfileImage",
                new { user.ID, user.Name, user.ScreenName, user.Location, user.Description, user.FollowerCount, user.FriendCount, user.CreatedAt, user.TimeZone, user.GeoEnabled, user.Verified, user.StatusCount, user.Lang, user.ProfileImage });
        }

        /// <summary>
        /// Ensures all given users are in the cache and only stores IDs.
        /// </summary>
        /// <seealso cref="UpdateUsers(IConfiguration, IEnumerable{TwitterUser})"/>
        /// <param name="configuration"></param>
        /// <param name="userIDs"></param>
        internal static void UpdateUsersByIDs(IConfiguration configuration, IEnumerable<string> userIDs)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var userID in userIDs)
                        session.WriteTransaction(tx => MergeUserID(tx, userID));
                }
            }
        }

        private static void MergeUserID(ITransaction tx, string userID)
        {
            tx.Run("MERGE (user:User {id: $ID}) ",
                new { ID = userID });
        }

        /// <summary>
        /// Returns a cached user's information if it exists in the database.
        /// </summary>
        /// <seealso cref="LookupUser(IConfiguration, string)"/>
        /// <param name="configuration"></param>
        /// <param name="screenName"></param>
        /// <returns></returns>
        internal static TwitterUser LookupUserByName(IConfiguration configuration, string screenName)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserByName(tx, screenName));
                }
            }
        }

        private static TwitterUser FindUserByName(ITransaction tx, string screenName)
        {
            return ToUserResult(tx.Run("MATCH (user:User) " +
                "WHERE user.screenName =~ $Regex " +
                "RETURN user", new { Regex = "(?i)" + screenName })
                .SingleOrDefault()?[0].As<INode>().Properties);
        }

        /// <summary>
        /// Returns a cached user's information by ID if it exists in the database.
        /// </summary>
        /// <seealso cref="LookupUserByName(IConfiguration, string)"/>
        /// <param name="configuration"></param>
        /// <param name="userID"></param>
        /// <returns></returns>
        internal static TwitterUser LookupUser(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUser(tx, userID));
                }
            }
        }

        private static TwitterUser FindUser(ITransaction tx, string userID)
        {
            return ToUserResult(tx.Run("MATCH (user:User {id: $ID}) " +
                "RETURN user", new { ID = userID })
                .SingleOrDefault()?[0].As<INode>().Properties);
        }

        /// <summary>
        /// Ensures the given user has cached connections to the given collection of users and caches any user info not already in the database.
        /// </summary>
        /// <seealso cref="UpdateUserConnections(IConfiguration, string, ICollection{string})"/>
        /// <param name="configuration"></param>
        /// <param name="queried">The user to update connections for.</param>
        /// <param name="users">The list of users connected to the queried user.</param>
        internal static void UpdateUserConnections(IConfiguration configuration, TwitterUser queried, ICollection<TwitterUser> users)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var user in users)
                        session.WriteTransaction(tx => UpdateUserConnection(tx, queried, user));
                    session.WriteTransaction(tx => MarkQueried(tx, queried.ID));
                }
            }
        }

        private static void UpdateUserConnection(ITransaction tx, TwitterUser queried, TwitterUser user)
        {
            tx.Run("MERGE (a:User {id: $ID}) " +
                "MERGE (b:User {id: $Other}) " +
                "MERGE (a)-[:FRIEND_FOLLOWER_OF]->(b) " +
                "MERGE (a)<-[:FRIEND_FOLLOWER_OF]-(b) " +
                "SET a.name = $aName, a.screenName = $aScreenName, a.location = $aLocation, " +
                "a.description = $aDescription, a.followerCount = $aFollowerCount, a.friendCount = $aFriendCount, " +
                "a.createdAt = $aCreatedAt, a.timeZone = $aTimeZone, a.geoEnabled = $aGeoEnabled, " +
                "a.verified = $aVerified, a.statusCount = $aStatusCount, a.lang = $aLang, a.profileImage = $aProfileImage " +
                "SET b.name = $bName, b.screenName = $bScreenName, b.location = $bLocation, " +
                "b.description = $bDescription, b.followerCount = $bFollowerCount, b.friendCount = $bFriendCount, " +
                "b.createdAt = $bCreatedAt, b.timeZone = $bTimeZone, b.geoEnabled = $bGeoEnabled, " +
                "b.verified = $bVerified, b.statusCount = $bStatusCount, b.lang = $bLang, b.profileImage = $bProfileImage ",
                new
                {
                    queried.ID,
                    Other = user.ID,
                    aName = queried.Name,
                    aScreenName = queried.ScreenName,
                    aLocation = queried.Location,
                    aDescription = queried.Description,
                    aFollowerCount = queried.FollowerCount,
                    aFriendCount = queried.FriendCount,
                    aCreatedAt = queried.CreatedAt,
                    aTimeZone = queried.TimeZone,
                    aGeoEnabled = queried.GeoEnabled,
                    aVerified = queried.Verified,
                    aStatusCount = queried.StatusCount,
                    aLang = queried.Lang,
                    aProfileImage = queried.ProfileImage,
                    bName = user.Name,
                    bScreenName = user.ScreenName,
                    bLocation = user.Location,
                    bDescription = user.Description,
                    bFollowerCount = user.FollowerCount,
                    bFriendCount = user.FriendCount,
                    bCreatedAt = user.CreatedAt,
                    bTimeZone = user.TimeZone,
                    bGeoEnabled = user.GeoEnabled,
                    bVerified = user.Verified,
                    bStatusCount = user.StatusCount,
                    bLang = user.Lang,
                    bProfileImage = user.ProfileImage
                });
        }

        /// <summary>
        /// Ensures the given user has cached connections to the given collection of users (by ID).
        /// </summary>
        /// <seealso cref="UpdateUserConnections(IConfiguration, TwitterUser, ICollection{TwitterUser})"/>
        /// <param name="configuration"></param>
        /// <param name="queried">The ID of the user to update connections for.</param>
        /// <param name="userIDs">The list of user IDs connected to the queried user.</param>
        internal static void UpdateUserConnections(IConfiguration configuration, string queried, IEnumerable<string> userIDs)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var id in userIDs)
                        session.WriteTransaction(tx => UpdateUserConnection(tx, queried, id));
                    session.WriteTransaction(tx => MarkQueried(tx, queried));
                }
            }
        }

        private static void MarkQueried(ITransaction tx, string userID)
        {
            tx.Run("MATCH (user:User {id: $ID}) " +
                "SET user.queried = true", new { ID = userID });
        }

        private static void UpdateUserConnection(ITransaction tx, string queried, string id)
        {
            tx.Run("MERGE (a:User {id: $ID}) " +
                "MERGE (b:User {id: $Other}) " +
                "MERGE (a)-[:FRIEND_FOLLOWER_OF]->(b) " +
                "MERGE (a)<-[:FRIEND_FOLLOWER_OF]-(b)",
                new { ID = queried, Other = id });
        }

        private static string FindUserConnection(ITransaction tx, string queriedID, string userID)
        {
            return tx.Run("MATCH (user:User {id: $ID)-[:FRIEND_FOLLOWER_OF]->(friend:User {id: $Other}) " +
                "RETURN friend",
                new { ID = queriedID, Other = userID })
                .SingleOrDefault()?.As<string>();
        }

        /// <summary>
        /// Finds all cached user connections for the given user.
        /// </summary>
        /// <seealso cref="FindUserConnections(IConfiguration, string)"/>
        /// <param name="configuration"></param>
        /// <param name="queried">The user to find connections for.</param>
        /// <returns></returns>
        internal static IEnumerable<TwitterUser> FindUserConnections(IConfiguration configuration, TwitterUser queried) => FindUserConnections(configuration, queried.ID);

        /// <summary>
        /// Finds all cached user connections for the given user ID.
        /// </summary>
        /// <seealso cref="FindUserConnections(IConfiguration, TwitterUser)"/>
        /// <param name="configuration"></param>
        /// <param name="userID">The ID of the user to find connections for.</param>
        /// <returns></returns>
        internal static IEnumerable<TwitterUser> FindUserConnections(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserConnections(tx, userID));
                }
            }
        }

        private static IEnumerable<TwitterUser> FindUserConnections(ITransaction tx, string userID)
        {
            return tx.Run("MATCH (user:User {id: $ID})-[:FRIEND_FOLLOWER_OF]->(friends) " +
                "RETURN friends",
                new { ID = userID })
                .Select(record =>
                {
                    var other = record[0].As<INode>().Properties;
                    return ToUserResult(other);
                });
        }

        /// <summary>
        /// Determines whether a cached path exists between the given objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="start">The starting node information to use with the query.</param>
        /// <param name="end">The ending node information to use with the query.</param>
        /// <param name="maxLength">The cap on the limit of considered paths.</param>
        /// <param name="label">The Neo4j node label corresponding to the node type.</param>
        /// <returns></returns>
        internal static bool PathExists<T>(IConfiguration configuration, T start, T end, int maxLength, string label) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    if (label == "User")
                        if (start is TwitterUser)
                            return session.ReadTransaction(tx => UserPathExists(tx, (start as TwitterUser).ID, (end as TwitterUser).ID, maxLength)).As<bool>();
                        else
                            return session.ReadTransaction(tx => UserPathExists(tx, start as string, end as string, maxLength)).As<bool>();
                    else
                        return session.ReadTransaction(tx => HashtagPathExists(tx, start as string, end as string, maxLength)).As<bool>();
                }
            }
        }

        private static bool UserPathExists(ITransaction tx, string start, string end, int maxLength)
        {
            return tx.Run("MATCH path=shortestPath((start:User {id: $start})-[*.." + maxLength + "]->(end:User {id: $end})) "
                + "RETURN path", new { start, end }).Count() > 0;
        }

        private static bool HashtagPathExists(ITransaction tx, string start, string end, int maxLength)
        {
            return tx.Run("MATCH path=shortestPath((start:Hashtag {text: $start})-[*.." + maxLength * 2 + "]-(end:Hashtag {text: $end})) " +
                   "RETURN path ", new { start, end }).Count() > 0;
        }

        private static TwitterUser ToUserResult(IReadOnlyDictionary<string, object> other)
        {
            if (other == null)
                return null;

            other.TryGetValue("name", out object name);
            other.TryGetValue("screenName", out object screenName);
            other.TryGetValue("location", out object location);
            other.TryGetValue("description", out object description);
            other.TryGetValue("followerCount", out object followerCount);
            other.TryGetValue("friendCount", out object friendCount);
            other.TryGetValue("createdAt", out object createdAt);
            other.TryGetValue("timeZone", out object timeZone);
            other.TryGetValue("geoEnabled", out object geoEnabled);
            other.TryGetValue("verified", out object verified);
            other.TryGetValue("statusCount", out object statusCount);
            other.TryGetValue("lang", out object lang);
            other.TryGetValue("profileImage", out object profileImage);
            return new TwitterUser()
            {
                ID = other["id"].As<string>(),
                Name = name?.As<string>(),
                ScreenName = screenName?.As<string>(),
                Location = location?.As<string>(),
                Description = description?.As<string>(),
                FollowerCount = followerCount?.As<long?>().GetValueOrDefault() ?? 0,
                FriendCount = friendCount?.As<long?>().GetValueOrDefault() ?? 0,
                CreatedAt = createdAt?.As<string>(),
                TimeZone = timeZone?.As<string>(),
                GeoEnabled = geoEnabled?.As<bool?>().GetValueOrDefault() ?? false,
                Verified = verified?.As<bool?>().GetValueOrDefault() ?? false,
                StatusCount = statusCount?.As<long?>().GetValueOrDefault() ?? 0,
                Lang = lang?.As<string>(),
                ProfileImage = profileImage?.As<string>()
            };
        }

        /// <summary>
        /// Returns whether the given user has had its connections queried and cached previously.
        /// </summary>
        /// <seealso cref="UserConnectionsQueried(IConfiguration, string)"/>
        /// <param name="configuration"></param>
        /// <param name="queried">The user to check cached connections for.</param>
        /// <returns></returns>
        internal static bool UserConnectionsQueried(IConfiguration configuration, TwitterUser queried) => UserConnectionsQueried(configuration, queried.ID);

        /// <summary>
        /// Returns whether the given user has had its connections queried and cached previously.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="queried">The ID of the user to check cached connections for.</param>
        /// <seealso cref="UserConnectionsQueried(IConfiguration, TwitterUser)"/>
        /// <returns></returns>
        internal static bool UserConnectionsQueried(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => UserQueried(tx, userID));
                }
            }
        }

        private static bool UserQueried(ITransaction tx, string userID)
        {
            bool? result = tx.Run("MATCH (user:User {id: $ID}) " +
                "RETURN user.queried", new { ID = userID })
                .SingleOrDefault()?[0].As<bool?>();
            if (result.HasValue)
                return result.Value;
            else
                return false;
        }

        /// <summary>
        /// Returns cached connected user IDs for the specified user ID.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="userID">The ID of the user to obtain cached connections for.</param>
        /// <returns></returns>
        internal static IEnumerable<string> FindUserConnectionIDs(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserConnectionIDs(tx, userID));
                }
            }
        }

        private static IEnumerable<string> FindUserConnectionIDs(ITransaction tx, string userID)
        {
            return tx.Run("MATCH (user:User {id: $ID})-[:FRIEND_FOLLOWER_OF]->(friends) " +
                "RETURN friends.id",
                new { ID = userID })
                .Select(record => record[0].As<string>());
        }

        /// <summary>
        /// Finds all shortest paths between the given start and end objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="start">The starting node information to use with the query.</param>
        /// <param name="end">The ending node information to use with the query.</param>
        /// <param name="maxLength">The cap on the limit of considered paths.</param>
        /// <param name="label">The Neo4j node label corresponding to the node type.</param>
        /// <returns></returns>
        internal static List<(List<Connection<T>.Node> Path, List<Status> Links)> ShortestPaths<T>(IConfiguration configuration, T start, T end, int maxLength, string label) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    if (label == "User")
                        if (start is TwitterUser)
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, (start as TwitterUser).ID, (end as TwitterUser).ID, maxLength))
                            .As<List<List<Connection<T>.Node>>>()
                            .Select(list => (list, new List<Status>()))
                            .ToList();
                        else
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, start as string, end as string, maxLength))
                            .As<List<List<Connection<TwitterUser>.Node>>>()
                            ?.Select(list => (list.Select(node => new Connection<T>.Node(node.Value.ID as T, node.Distance)).ToList(), new List<Status>()))
                            .ToList();
                    else
                        return session.ReadTransaction(tx => ShortestHashtagPaths(tx, start as string, end as string, maxLength))
                            .As<List<(List<Connection<T>.Node>, List<Status>)>>();
                }
            }
        }

        private static List<List<Connection<TwitterUser>.Node>> ShortestUserPaths(ITransaction tx, string start, string end, int maxLength)
        {
            return tx.Run("MATCH path=allShortestPaths((start:User {id: $start})-[*.." + maxLength + "]->(end:User {id: $end})) "
                + "RETURN path",
                new { start, end })
                .Select(record => record[0]
                .As<IPath>()
                .Nodes
                .Select((node, index) => new Connection<TwitterUser>.Node(ToUserResult(node.Properties), index))
                .ToList())
                .ToList();
        }

        private static List<(List<Connection<string>.Node> Path, List<Status> Links)> ShortestHashtagPaths(ITransaction tx, string start, string end, int maxLength)
        {
            // Multiply max length by two since a hashtag-to-hashtag connection passes through a status node
            return tx.Run("MATCH path=allShortestPaths((start:Hashtag {text: $start})-[*.." + maxLength * 2 + "]-(end:Hashtag {text: $end})) " +
                "RETURN path " +
                "LIMIT 100",
                new { start, end })
                .Select(ToPathWithLinks())
                .Distinct(new PathEqualityComparer())
                .ToList();
        }

        private class PathEqualityComparer : EqualityComparer<(List<Connection<string>.Node> Path, List<Status> Links)>
        {
            public override bool Equals((List<Connection<string>.Node> Path, List<Status> Links) x, (List<Connection<string>.Node> Path, List<Status> Links) y)
            {
                if (x.Path.Count != y.Path.Count)
                    return false;
                for (int i = 0; i < x.Path.Count; ++i)
                    if (!x.Path[i].Value.Equals(y.Path[i].Value))
                        return false;
                return true;
            }

            public override int GetHashCode((List<Connection<string>.Node> Path, List<Status> Links) obj)
            {
                return obj.Path
                    .Select(node => node.Value.GetHashCode())
                    .Aggregate((one, two) => one.GetHashCode() ^ two.GetHashCode());
            }
        }

        private static Func<IRecord, (List<Connection<string>.Node>, List<Status>)> ToPathWithLinks() =>
            record => (GetPath(record), GetLinks(record));

        private static List<Status> GetLinks(IRecord record)
        {
            return record[0]
                .As<IPath>()
                .Nodes
                .Where(node => node.Labels.Contains("Status"))
                .Select(node => ToStatus(node.Properties))
                .ToList();
        }

        private static List<Connection<string>.Node> GetPath(IRecord record)
        {
            return record[0]
                .As<IPath>()
                .Nodes
                .Where(node => node.Labels.Contains("Hashtag"))
                .Select((node, index) => new Connection<string>.Node(node.Properties["text"].ToString(), index))
                .ToList();
        }

        /// <summary>
        /// Ensures the given connections are cached for the specified hashtag.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="start">The hashtag to update connections for.</param>
        /// <param name="link">The tweet that linked the hashtags.</param>
        /// <param name="connections">The list of hashtags the given starting tag is used with.</param>
        internal static void UpdateHashtagConnections(IConfiguration configuration, string start, Status link, IEnumerable<string> connections)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (string connection in connections)
                        session.WriteTransaction(tx => UpdateHashtagConnection(tx, start, link, connection));
                    session.WriteTransaction(tx => MarkHashtagQueried(tx, start));
                }
            }
        }

        private static void UpdateHashtagConnection(ITransaction tx, string start, Status status, string other)
        {
            tx.Run("MERGE (a:Hashtag {text: $Text}) " +
                "MERGE (b:Hashtag {text: $Other}) " +
                "MERGE (a)-[:TWEETED_IN]->(status:Status {idstr: $StatusIdStr})<-[:TWEETED_IN]-(b) " +
                "SET status.favoriteCount = $FavoriteCount, status.inReplyToScreenName = $InReplyToScreenName, " +
                "status.inReplyToStatusIdStr = $InReplyToStatusIdStr, status.inReplyToUserIdStr = $InReplyToUserIdStr, " +
                "status.possiblySensitive = $PossiblySensitive, status.retweetCount = $RetweetCount, " +
                "status.retweeted = $Retweeted, status.source = $Source, status.text = $StatusText, " +
                "status.truncated = $Truncated, status.userScreenName = $UserScreenName, " +
                "status.userIdStr = $UserIdStr",
                new
                {
                    Text = start,
                    Other = other,
                    StatusIdStr = status.IdStr,
                    status.FavoriteCount,
                    status.InReplyToScreenName,
                    status.InReplyToStatusIdStr,
                    status.InReplyToUserIdStr,
                    status.PossiblySensitive,
                    status.RetweetCount,
                    status.Retweeted,
                    status.Source,
                    StatusText = status.Text,
                    status.Truncated,
                    UserScreenName = status.User.ScreenName,
                    UserIdStr = status.User.IdStr
                });
        }

        private static void MarkHashtagQueried(ITransaction tx, string hashtag)
        {
            tx.Run("MATCH (tag:Hashtag {text: $Text}) " +
                "SET tag.queried = true", new { Text = hashtag });
        }

        /// <summary>
        /// Returns whether the given hashtag has had its connections queried and cached previously.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="hashtag">The hashtag to check cached connections for.</param>
        /// <returns></returns>
        internal static bool HashtagConnectionsQueried(IConfiguration configuration, string hashtag)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => HashtagQueried(tx, hashtag));
                }
            }
        }

        private static bool HashtagQueried(ITransaction tx, string hashtag)
        {
            bool? result = tx.Run("MATCH (tag:Hashtag {text: $Text}) " +
                "RETURN tag.queried", new { Text = hashtag })
                .SingleOrDefault()?[0].As<bool?>();
            if (result.HasValue)
                return result.Value;
            else
                return false;
        }

        /// <summary>
        /// Returns other cached hashtag connections for the given hashtag.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="hashtag">The hashtag to search for.</param>
        /// <param name="max">The maximum number of hashtags to return.</param>
        /// <returns></returns>
        internal static IDictionary<Status, IEnumerable<string>> FindHashtagConnections(IConfiguration configuration, string hashtag, int max)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindHashtagConnections(tx, hashtag, max));
                }
            }
        }

        private static IDictionary<Status, IEnumerable<string>> FindHashtagConnections(ITransaction tx, string hashtag, int max)
        {
            return tx.Run("MATCH (start:Hashtag {text: $Text})-[:TWEETED_IN]->(status:Status)<-[TWEETED_IN]-(other) " +
                "WHERE other.text <> $Text " +
                "RETURN status, other.text LIMIT " + max,
                new { Text = hashtag })
                .Select(record => new
                {
                    Status = ToStatus(record[0].As<INode>().Properties),
                    Tag = record[1].As<string>()
                })
                .GroupBy(link => link.Status)
                .ToDictionary(group => (group.Key), group => group.AsEnumerable().Select(val => val.Tag));
        }

        private static Status ToStatus(IReadOnlyDictionary<string, object> properties)
        {
            if (properties == null)
                return null;

            properties.TryGetValue("favoriteCount", out object favoriteCount);
            properties.TryGetValue("inReplyToScreenName", out object inReplyToScreenName);
            properties.TryGetValue("inReplyToStatusIdStr", out object inReplyToStatusIdStr);
            properties.TryGetValue("inReplyToUserIdStr", out object inReplyToUserIdStr);
            properties.TryGetValue("possiblySensitive", out object possiblySensitive);
            properties.TryGetValue("retweetCount", out object retweetCount);
            properties.TryGetValue("retweeted", out object retweeted);
            properties.TryGetValue("source", out object source);
            properties.TryGetValue("text", out object text);
            properties.TryGetValue("truncated", out object truncated);
            properties.TryGetValue("userScreenName", out object userScreenName);
            properties.TryGetValue("userIdStr", out object userIdStr);
            return new Status()
            {
                IdStr = properties["idstr"].As<string>(),
                FavoriteCount = favoriteCount?.As<long>(),
                InReplyToScreenName = inReplyToScreenName?.As<string>(),
                InReplyToStatusIdStr = inReplyToStatusIdStr?.As<string>(),
                InReplyToUserIdStr = inReplyToUserIdStr?.As<string>(),
                PossiblySensitive = possiblySensitive?.As<bool?>().GetValueOrDefault(),
                RetweetCount = retweetCount?.As<long>() ?? 0,
                Retweeted = retweeted?.As<bool>() ?? false,
                Source = source?.As<string>(),
                Text = text?.As<string>(),
                Truncated = truncated?.As<bool>() ?? false,
                User = new UserSearchResults() { IdStr = userIdStr?.As<string>(), ScreenName = userScreenName?.As<string>() }
            };
        }
    }
}
