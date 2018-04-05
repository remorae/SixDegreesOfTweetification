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
        internal static void UpdateUsers(IConfiguration config, IEnumerable<UserResult> users)
        {
            using (IDriver driver = GraphDatabase.Driver(config["twitterCacheURI"], AuthTokens.Basic(config["twitterCacheUser"], config["twitterCachePassword"])))
            {
                ICollection<UserResult> toStore = new List<UserResult>();
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    foreach (var user in users.Where(u => session.ReadTransaction(tx => FindUser(tx, u)) == null))
                        toStore.Add(user);
                }
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var user in toStore)
                        session.WriteTransaction(tx => CreateUser(tx, user));
                }
            }
        }

        private static string FindUser(ITransaction tx, UserResult user) => FindUser(tx, user.ID);

        private static string FindUser(ITransaction tx, string userID)
        {
            return tx.Run("MATCH (user:User {id: $ID}) " +
                "RETURN user", new { ID = userID })
                .SingleOrDefault()?[0].As<string>();
        }

        private static void CreateUser(ITransaction tx, UserResult user)
        {
            tx.Run("CREATE (user:User) " +
                "SET user.id = $ID, user.name = $Name, user.screenName = $ScreenName, user.location = $Location, " +
                "user.description = $Description, user.followerCount = $FollowerCount, user.friendCount = $FriendCount, " +
                "user.createdAt = $CreatedAt, user.timeZone = $TimeZone, user.geoEnabled = $GeoEnabled, " +
                "user.verified = $Verified, user.statusCount = $StatusCount, user.lang = $Lang, user.profileImage = $ProfileImage",
                new { user.ID, user.Name, user.ScreenName, user.Location, user.Description, user.FollowerCount, user.FriendCount, user.CreatedAt, user.TimeZone, user.GeoEnabled, user.Verified, user.StatusCount, user.Lang, user.ProfileImage });
        }

        internal static void UpdateUserConnections(IConfiguration configuration, UserResult queried, ICollection<UserResult> users)
        {
            using (IDriver driver = GraphDatabase.Driver(configuration["twitterCacheURI"], AuthTokens.Basic(configuration["twitterCacheUser"], configuration["twitterCachePassword"])))
            {
                ICollection<UserResult> toStore = new List<UserResult>();
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    foreach (var user in users.Where(u => session.ReadTransaction(tx => FindUserConnection(tx, queried, u)) == null))
                        toStore.Add(user);
                }
                using (ISession session = driver.Session(AccessMode.Write))
                {
                    foreach (var user in toStore)
                        session.WriteTransaction(tx => AddUserConnection(tx, queried, user));
                    session.WriteTransaction(tx => MarkQueried(tx, queried));
                }
            }
        }

        private static void MarkQueried(ITransaction tx, UserResult queried) => MarkQueried(tx, queried.ID);

        private static void MarkQueried(ITransaction tx, string userID)
        {
            tx.Run("MATCH (user:User {id: $ID}) " +
                "SET user.queried = true", new { ID = userID });
        }

        private static void AddUserConnection(ITransaction tx, UserResult queried, UserResult user)
        {
            tx.Run("MATCH (a:User),(b:User) " +
                "WHERE a.id = $ID AND b.id = $Other " +
                "CREATE (a)-[:FRIEND_FOLLOWER_OF]->(b) " +
                "CREATE (b)-[:FRIEND_FOLLOWER_OF]->(a)",
                new { queried.ID, Other = user.ID });
        }

        private static string FindUserConnection(ITransaction tx, UserResult queried, UserResult user)
        {
            return tx.Run("MATCH (user:User {id: $ID})-[:FRIEND_FOLLOWER_OF]->(friend:User) " +
                "WHERE friend.id = $Other " +
                "RETURN friend",
                new { queried.ID, Other = user.ID })
                .SingleOrDefault()?.As<string>();
        }

        internal static IEnumerable<UserResult> FindUserConnections(IConfiguration configuration, UserResult queried)
        {
            using (IDriver driver = GraphDatabase.Driver(configuration["twitterCacheURI"], AuthTokens.Basic(configuration["twitterCacheUser"], configuration["twitterCachePassword"])))
            {
                using (ISession session = driver.Session(AccessMode.Read))
                {
                    return session.ReadTransaction(tx => FindUserConnections(tx, queried));
                }
            }
        }

        private static IEnumerable<UserResult> FindUserConnections(ITransaction tx, UserResult queried)
        {
            return tx.Run("MATCH (user:User {id: $ID})-[:FRIEND_FOLLOWER_OF]->(friend:User) " +
                "RETURN friend",
                new { queried.ID })
                .Select(record => new UserResult()
                {
                    ID = record["ID"].As<string>(),
                    Name = record["Name"].As<string>(),
                    ScreenName = record["ScreenName"].As<string>(),
                    Location = record["Location"].As<string>(),
                    Description = record["Description"].As<string>(),
                    FollowerCount = record["FollowerCount"].As<long>(),
                    FriendCount = record["FriendCount"].As<long>(),
                    CreatedAt = record["CreatedAt"].As<string>(),
                    TimeZone = record["TimeZone"].As<string>(),
                    GeoEnabled = record["GeoEnabled"].As<bool>(),
                    Verified = record["Verified"].As<bool>(),
                    StatusCount = record["StatusCount"].As<long>(),
                    Lang = record["Lang"].As<string>(),
                    ProfileImage = record["ProfileImage"].As<string>()
                });
        }
    }
}
