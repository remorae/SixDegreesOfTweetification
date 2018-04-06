using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver.V1;
using SixDegrees.Model;

namespace SixDegrees.Data
{
    public class TwitterCache
    {
        private static IDriver GetDriver(IConfiguration config) =>
            GraphDatabase.Driver(config["twitterCacheURI"], AuthTokens.Basic(config["twitterCacheUser"], config["twitterCachePassword"]));

        internal static void UpdateUsers(IConfiguration config, IEnumerable<UserResult> users)
        {
            using (IDriver driver = GetDriver(config))
            {
                ICollection<UserResult> toStore = new List<UserResult>();
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var user in users.Except(toStore))
                        session.WriteTransaction(tx => UpdateUser(tx, user));
                }
            }
        }

        private static void UpdateUser(ITransaction tx, UserResult user)
        {
            tx.Run("MERGE (user:User {id: $ID}) " +
                "SET user.name = $Name, user.screenName = $ScreenName, user.location = $Location, " +
                "user.description = $Description, user.followerCount = $FollowerCount, user.friendCount = $FriendCount, " +
                "user.createdAt = $CreatedAt, user.timeZone = $TimeZone, user.geoEnabled = $GeoEnabled, " +
                "user.verified = $Verified, user.statusCount = $StatusCount, user.lang = $Lang, user.profileImage = $ProfileImage",
                new { user.ID, user.Name, user.ScreenName, user.Location, user.Description, user.FollowerCount, user.FriendCount, user.CreatedAt, user.TimeZone, user.GeoEnabled, user.Verified, user.StatusCount, user.Lang, user.ProfileImage });
        }

        internal static void UpdateUsersByIDs(IConfiguration config, IEnumerable<string> userIDs)
        {
            using (IDriver driver = GetDriver(config))
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

        internal static UserResult LookupUserByName(IConfiguration config, string screenName)
        {
            using (IDriver driver = GetDriver(config))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserByName(tx, screenName));
                }
            }
        }

        private static UserResult FindUserByName(ITransaction tx, string screenName)
        {
            return ToUserResult(tx.Run("MATCH (user:User) " +
                "WHERE user.screenName =~ $Regex " +
                "RETURN user", new { Regex = "(?i)" + screenName })
                .SingleOrDefault()?[0].As<INode>().Properties);
        }

        internal static UserResult LookupUser(IConfiguration config, string userID)
        {
            using (IDriver driver = GetDriver(config))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUser(tx, userID));
                }
            }
        }

        private static UserResult FindUser(ITransaction tx, string userID)
        {
            return ToUserResult(tx.Run("MATCH (user:User {id: $ID}) " +
                "RETURN user", new { ID = userID })
                .SingleOrDefault()?[0].As<INode>().Properties);
        }

        internal static void UpdateUserConnections(IConfiguration configuration, UserResult queried, ICollection<UserResult> users) =>
            UpdateUserConnections(configuration, queried.ID, users.Select(user => user.ID));

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
                "MERGE (b)-[:FRIEND_FOLLOWER_OF]->(a)",
                new { ID = queried, Other = id });
        }

        private static string FindUserConnection(ITransaction tx, string queriedID, string userID)
        {
            return tx.Run("MATCH (user:User {id: $ID)-[:FRIEND_FOLLOWER_OF]->(friend:User {id: $Other}) " +
                "RETURN friend",
                new { ID = queriedID, Other = userID })
                .SingleOrDefault()?.As<string>();
        }

        internal static IEnumerable<UserResult> FindUserConnections(IConfiguration configuration, UserResult queried) => FindUserConnections(configuration, queried.ID);

        internal static IEnumerable<UserResult> FindUserConnections(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserConnections(tx, userID));
                }
            }
        }

        private static IEnumerable<UserResult> FindUserConnections(ITransaction tx, string userID)
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

        private static UserResult ToUserResult(IReadOnlyDictionary<string, object> other)
        {
            if (other == null)
                return null;

            other.TryGetValue("timeZone", out object name);
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
            return new UserResult()
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

        internal static bool UserConnectionsQueried(IConfiguration configuration, UserResult queried) => UserConnectionsQueried(configuration, queried.ID);

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

        internal static List<List<ConnectionInfo<T>.Node>> ShortestPaths<T>(IConfiguration configuration, T start, T end, int maxLength, string label) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    if (label == "User")
                        if (start is UserResult)
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, (start as UserResult).ID, (end as UserResult).ID, maxLength))
                            .As<List<List<ConnectionInfo<T>.Node>>>();
                        else
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, start as string, end as string, maxLength))
                            .As<List<List<ConnectionInfo<UserResult>.Node>>>()
                            ?.Select(list => list.Select(node => new ConnectionInfo<T>.Node(node.Value.ID as T, node.Distance)).ToList())
                            .ToList();
                    else
                        return session.ReadTransaction(tx => ShortestHashtagPaths(tx, start as string, end as string, maxLength))
                            .As<List<List<ConnectionInfo<T>.Node>>>();
                }
            }
        }

        private static List<List<ConnectionInfo<string>.Node>> ShortestHashtagPaths(ITransaction tx, string start, string end, int maxLength)
        {
            return tx.Run("MATCH path=allShortestPaths((start:Hashtag {text: $start})-[*.." + maxLength + "]-(end:Hashtag {text: $end})) "
                + "RETURN path",
                new { start, end })
                .Select(record => record[0]
                .As<IPath>()
                .Nodes
                .Select((node, index) => new ConnectionInfo<string>.Node(node.Properties["text"].ToString(), index))
                .ToList())
                .ToList();
        }

        private static List<List<ConnectionInfo<UserResult>.Node>> ShortestUserPaths(ITransaction tx, string start, string end, int maxLength)
        {
            return tx.Run("MATCH path=allShortestPaths((start:User {id: $start})-[*.." + maxLength + "]->(end:User {id: $end})) "
                + "RETURN path",
                new { start, end })
                .Select(record => record[0]
                .As<IPath>()
                .Nodes
                .Select((node, index) => new ConnectionInfo<UserResult>.Node(ToUserResult(node.Properties), index))
                .ToList())
                .ToList();
        }

        internal static IEnumerable<T> Shared<T>(IConfiguration configuration, T start, T end) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => SharedConnections(tx, start, end));
                }
            }
        }

        private static IEnumerable<T> SharedConnections<T>(ITransaction tx, T start, T end) where T : class
        {
            return Enumerable.Empty<T>(); //TODO
        }
    }
}
