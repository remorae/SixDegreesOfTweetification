using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;
using SixDegrees.Model.JSON;

namespace SixDegrees.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private const int TWEET_COUNT = 100;
        private const string TWEET_MODE = "extended";
        private const bool INCLUDE_ENTITIES = true;
        private const string CONTENT_TYPE = "application/x-www-form-urlencoded";

        private Dictionary<RepeatQueryType, QueryInfo> history = new Dictionary<RepeatQueryType, QueryInfo>();

        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
            InitHistory();
        }

        /// <summary>
        /// Returns a list of tweets containing given hashtags
        /// </summary>
        /// <param name="query">The hashtags to search for, separated by spaces</param>
        /// <returns></returns>
        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            string responseBody = await GetSearchResults(TweetSearchAPIUri(HashtagSearchQuery(query, RepeatQueryType.TweetsByHashtag)));
            if (responseBody == null)
                return null;
            var results = TweetSearchResults.FromJson(responseBody);
            LogQuery(query, results, RepeatQueryType.TweetsByHashtag);

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
            string responseBody = await GetSearchResults(TweetSearchAPIUri(HashtagSearchQuery(query, RepeatQueryType.LocationsByHashtag)));
            if (responseBody == null)
                return null;
            var results = TweetSearchResults.FromJson(responseBody);
            LogQuery(query, results, RepeatQueryType.LocationsByHashtag);

            Dictionary<string, Country> countries = new Dictionary<string, Country>();
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

        private void UpdateCountriesWithPlace(Dictionary<string, Country> countries, Status status)
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
            return countries.Select(country =>
            {
                Dictionary<string, List<PlaceResult>> placeCategories = new Dictionary<string, List<PlaceResult>>();
                foreach (PlaceResult place in country.Places.Values)
                {
                    string placeTypeString = place.Type.ToString();
                    if (!placeCategories.ContainsKey(placeTypeString))
                        placeCategories.Add(placeTypeString, new List<PlaceResult>());
                    placeCategories[placeTypeString].Add(place);
                }
                return new CountryResult(country.CountryName, placeCategories);
            });
        }

        /// <summary>
        /// Returns information about a specified Twitter user
        /// </summary>
        /// <param name="query">The user screen name to search for</param>
        /// <returns></returns>
        [HttpGet("user")]
        public async Task<UserSearchResults> GetUser(string query)
        {
            string responseBody = await GetSearchResults(UserSearchAPIUri(UserSearchQuery(query)));
            if (responseBody == null)
                return null;
            var results = UserSearchResults.FromJson(responseBody);
            return results;
        }

        #region Twitter API
        private Uri UserSearchAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/users/show.json")
            {
                Query = query
            };
            return bob.Uri;
        }

        private Uri TweetSearchAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/search/tweets.json")
            {
                Query = query
            };
            return bob.Uri;
        }

        private string UserSearchQuery(string screenName)
        {
            return $"screen_name={screenName}&include_entities={INCLUDE_ENTITIES}";
        }

        private string HashtagSearchQuery(string hashtag, RepeatQueryType type)
        {
            string result = $"q=%23{hashtag}&count={TWEET_COUNT}&tweet_mode={TWEET_MODE}&include_entities={INCLUDE_ENTITIES}";
            if (hashtag == history[type].LastQuery && history[type].LastMaxID != "")
                result += $"&max_id={history[type].LastMaxID}";
            return result;
        }

        private void AddBearerAuth(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {Configuration["bearerToken"]}");
        }
        #endregion

        private async Task<string> GetSearchResults(Uri uri)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        AddBearerAuth(request);
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

        #region History
        private void InitHistory()
        {
            history.Add(RepeatQueryType.TweetsByHashtag, new QueryInfo());
            history.Add(RepeatQueryType.LocationsByHashtag, new QueryInfo());
        }

        private void LogQuery(string query, TweetSearchResults results, RepeatQueryType type)
        {
            history[type] = new QueryInfo(query, (long.Parse(results.Statuses.Min(status => status.IdStr)) - 1).ToString());
        }
        #endregion
    }
}
