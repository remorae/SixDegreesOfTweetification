using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Neo4j.Driver.V1;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Data
{
    public class TwitterCache
    {
        private static IDriver GetDriver(IConfiguration configuration) =>
            GraphDatabase.Driver(configuration["twitterCacheURI"],
                AuthTokens.Basic(configuration["twitterCacheUser"], configuration["twitterCachePassword"]));

        internal static void UpdateUsers(IConfiguration configuration, IEnumerable<UserResult> users)
        {
            using (IDriver driver = GetDriver(configuration))
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

        internal static UserResult LookupUserByName(IConfiguration configuration, string screenName)
        {
            using (IDriver driver = GetDriver(configuration))
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

        internal static UserResult LookupUser(IConfiguration configuration, string userID)
        {
            using (IDriver driver = GetDriver(configuration))
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

        internal static void UpdateUserConnections(IConfiguration configuration, UserResult queried, ICollection<UserResult> users)
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

        private static void UpdateUserConnection(ITransaction tx, UserResult queried, UserResult user)
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

        internal static bool PathExists<T>(IConfiguration configuration, T start, T end, int maxLength, string label) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    if (label == "User")
                        if (start is UserResult)
                            return session.ReadTransaction(tx => UserPathExists(tx, (start as UserResult).ID, (end as UserResult).ID, maxLength)).As<bool>();
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

        private static UserResult ToUserResult(IReadOnlyDictionary<string, object> other)
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

        internal static List<(List<ConnectionInfo<T>.Node> Path, List<Status> Links)> ShortestPaths<T>(IConfiguration configuration, T start, T end, int maxLength, string label) where T : class
        {
            using (IDriver driver = GetDriver(configuration))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    if (label == "User")
                        if (start is UserResult)
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, (start as UserResult).ID, (end as UserResult).ID, maxLength))
                            .As<List<List<ConnectionInfo<T>.Node>>>()
                            .Select(list => (list, new List<Status>()))
                            .ToList();
                        else
                            return session.ReadTransaction(tx => ShortestUserPaths(tx, start as string, end as string, maxLength))
                            .As<List<List<ConnectionInfo<UserResult>.Node>>>()
                            ?.Select(list => (list.Select(node => new ConnectionInfo<T>.Node(node.Value.ID as T, node.Distance)).ToList(), new List<Status>()))
                            .ToList();
                    else
                        return session.ReadTransaction(tx => ShortestHashtagPaths(tx, start as string, end as string, maxLength))
                            .As<List<(List<ConnectionInfo<T>.Node>, List<Status>)>>();
                }
            }
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

        private static List<(List<ConnectionInfo<string>.Node> Path, List<Status> Links)> ShortestHashtagPaths(ITransaction tx, string start, string end, int maxLength)
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

        private class PathEqualityComparer : EqualityComparer<(List<ConnectionInfo<string>.Node> Path, List<Status> Links)>
        {
            public override bool Equals((List<ConnectionInfo<string>.Node> Path, List<Status> Links) x, (List<ConnectionInfo<string>.Node> Path, List<Status> Links) y)
            {
                if (x.Path.Count != y.Path.Count)
                    return false;
                for (int i = 0; i < x.Path.Count; ++i)
                    if (!x.Path[i].Value.Equals(y.Path[i].Value))
                        return false;
                return true;
            }

            public override int GetHashCode((List<ConnectionInfo<string>.Node> Path, List<Status> Links) obj)
            {
                return obj.Path
                    .Select(node => node.Value.GetHashCode())
                    .Aggregate((one, two) => one.GetHashCode() ^ two.GetHashCode());
            }
        }

        private static Func<IRecord, (List<ConnectionInfo<string>.Node>, List<Status>)> ToPathWithLinks() =>
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

        private static List<ConnectionInfo<string>.Node> GetPath(IRecord record)
        {
            return record[0]
                .As<IPath>()
                .Nodes
                .Where(node => node.Labels.Contains("Hashtag"))
                .Select((node, index) => new ConnectionInfo<string>.Node(node.Properties["text"].ToString(), index))
                .ToList();
        }

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
