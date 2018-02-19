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
    class SearchController : Controller
    {
        private IConfiguration Configuration { get; }

        internal SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private async Task<T> GetResults<T>(QueryType queryType, string query, AuthenticationType authType, Func<string, Uri> buildUri, Func<string, QueryType, string> buildQuery) where T : IQueryResults
        {
            string responseBody = await TwitterAPIUtils.GetResponse(Configuration, authType, buildUri(buildQuery(query, queryType)), queryType);
            if (responseBody == null)
                return default(T);
            T results = JsonConvert.DeserializeObject<T>(responseBody);
            LogQuery(query, queryType, results);
            return results;
        }

        private void LogQuery(string query, QueryType type, IQueryResults results)
        {
            QueryHistory.Get[type].LastQuery = query;
            if (QueryInfo.UsesMaxID(type))
            {
                TweetSearchResults statusResults = results as TweetSearchResults;
                // Exclude lowest ID to prevent duplicate results
                string lastMaxID = (long.TryParse(statusResults.MinStatusID, out long result)) ? (result - 1).ToString() : "";
                QueryHistory.Get[type].LastQuery = lastMaxID;
            }
        }

        /// <summary>
        /// Returns a list of tweets containing given hashtags
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            var results = await GetResults<TweetSearchResults>(QueryType.TweetsByHashtag, query, AuthenticationType.Application, TwitterAPIUtils.TweetSearchAPIUri, TwitterAPIUtils.HashtagSearchQuery);
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
            var results = await GetResults<TweetSearchResults>(QueryType.LocationsByHashtag, query, AuthenticationType.Application, TwitterAPIUtils.TweetSearchAPIUri, TwitterAPIUtils.HashtagSearchQuery);
            if (results == null)
                return null;
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

        private void UpdateCountriesWithPlace(IDictionary<string, Country> countries, Status status)
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
            var results = await GetResults<UserSearchResults>(QueryType.UserByScreenName, screen_name, AuthenticationType.Application, TwitterAPIUtils.UserSearchAPIUri, TwitterAPIUtils.UserSearchQuery);
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
