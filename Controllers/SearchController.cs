using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private static void LogQuery(string query, TwitterAPIEndpoint endpoint, IQueryResults results)
        {
            QueryHistory.Get[endpoint].LastQuery = query;
            if (QueryInfo.UsesMaxID(endpoint))
            {
                TweetSearchResults statusResults = results as TweetSearchResults;
                // Exclude lowest ID to prevent duplicate results
                string lastMaxID = (long.TryParse(statusResults.MinStatusID, out long result)) ? (result - 1).ToString() : "";
                QueryHistory.Get[endpoint].LastMaxID = lastMaxID;
            }
        }

        private static void UpdateCountriesWithPlace(IDictionary<string, Country> countries, Status status)
        {
            string placeName = status.Place.FullName;

            string countryName = status.Place.Country;
            if (!countries.ContainsKey(countryName))
                countries[countryName] = new Country(countryName);
            if (!countries[countryName].Places.ContainsKey(placeName))
            {
                PlaceResult toAdd = new PlaceResult(placeName, status.Place.PlaceType.ToPlaceType(), countryName);
                countries[countryName].Places[placeName] = toAdd;
            }
            countries[countryName].Places[placeName].Sources.Add(status.URL);
            foreach (Hashtag tag in status.Entities.Hashtags)
            {
                if (!countries[countryName].Places[placeName].Hashtags.Contains(tag.Text))
                    countries[countryName].Places[placeName].Hashtags.Add(tag.Text);
            }
        }

        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private async Task<T> GetResults<T>(string query, AuthenticationType authType, Func<string, TwitterAPIEndpoint, string> buildQueryString, TwitterAPIEndpoint endpoint, string token) where T : IQueryResults
        {
            string responseBody = await TwitterAPIUtils.GetResponse(Configuration, authType, endpoint, buildQueryString(query, endpoint), token);
            if (responseBody == null)
                return default(T);
            T results = JsonConvert.DeserializeObject<T>(responseBody);
            LogQuery(query, endpoint, results);
            return results;
        }

        /// <summary>
        /// Returns a list of tweets containing given hashtags
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            //TODO Use user token
            var results = await GetResults<TweetSearchResults>(query, AuthenticationType.Both, TwitterAPIUtils.HashtagSearchQuery, TwitterAPIEndpoint.SearchTweets, null);
            if (results == null)
                return null;
            return results.Statuses;
        }

        /// <summary>
        /// Returns a list of locations from tweets containing given hashtags
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces</param>
        /// <returns></returns>
        [HttpGet("locations")]
        public async Task<IEnumerable<CountryResult>> Locations(string query)
        {
            //TODO Use user token
            var results = await GetResults<TweetSearchResults>(query, AuthenticationType.Both, TwitterAPIUtils.HashtagSearchQuery, TwitterAPIEndpoint.SearchTweets, null);
            if (results == null)
                return Enumerable.Empty<CountryResult>();
            IDictionary<string, Country> countries = new Dictionary<string, Country>();
            foreach (Status status in results.Statuses)
            {
                if (status.Place != null)
                    UpdateCountriesWithPlace(countries, status);
                else if (status.Coordinates != null && status.Coordinates.Type == "Point")
                {
                    //TODO - Look up city/country names based on longitude/latitude
                }
            }
            return GetFormattedCountries(countries.Values);
        }

        private IEnumerable<CountryResult> GetFormattedCountries(IEnumerable<Country> countries)
        {
            return countries.Select(country => new CountryResult(country.Name, country.Places.Values));
        }

        /// <summary>
        /// Returns information about a specified Twitter user
        /// </summary>
        /// <param name="query">The user screen name to search for</param>
        /// <returns></returns>
        [HttpGet("user")]
        public async Task<UserResult> GetUser(string screen_name)
        {
            //TODO Use user token
            var results = await GetResults<UserSearchResults>(screen_name, AuthenticationType.Both, TwitterAPIUtils.UserSearchQuery, TwitterAPIEndpoint.UsersShow, null);
            if (results == null)
                return null;
            return new UserResult()
            {
                CreatedAt = results.CreatedAt,
                Description = results.Description,
                FollowerCount = results.FollowersCount,
                FriendCount = results.FriendsCount,
                GeoEnabled = results.GeoEnabled,
                ID = results.IdStr,
                Lang = results.Lang,
                Location = results.Location,
                Name = results.Name,
                ProfileImage = results.ProfileImageUrlHttps,
                ScreenName = results.ScreenName,
                StatusCount = results.StatusesCount,
                TimeZone = results.TimeZone,
                Verified = results.Verified
            };
        }
    }
}
