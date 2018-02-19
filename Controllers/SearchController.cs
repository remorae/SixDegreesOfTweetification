using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private async Task<T> GetResults<T>(QueryType type, string query, Func<string, Uri> buildUri, Func<string, QueryType, string> buildQuery) where T : IQueryResults
        {
            string responseBody = await GetResponse(buildUri(buildQuery(query, QueryType.TweetsByHashtag)));
            if (responseBody == null)
                return default(T);
            T results = JsonConvert.DeserializeObject<T>(responseBody);
            LogQuery(query, type, results);
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

        private async Task<string> GetResponse(Uri uri)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        TwitterAPIUtils.AddBearerAuth(Configuration, request);
                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                        {
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
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
            var results = await GetResults<TweetSearchResults>(QueryType.TweetsByHashtag, query, TwitterAPIUtils.TweetSearchAPIUri, TwitterAPIUtils.HashtagSearchQuery);
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
            var results = await GetResults<TweetSearchResults>(QueryType.LocationsByHashtag, query, TwitterAPIUtils.TweetSearchAPIUri, TwitterAPIUtils.HashtagSearchQuery);
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
        public async Task<UserSearchResults> GetUser(string query)
        {
            var results = await GetResults<UserSearchResults>(QueryType.UserByScreenName, query, TwitterAPIUtils.UserSearchAPIUri, TwitterAPIUtils.UserSearchQuery);
            if (results == null)
                return null;
            return results;
        }
    }
}
