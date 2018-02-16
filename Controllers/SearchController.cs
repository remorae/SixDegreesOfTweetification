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

        private void InitHistory()
        {
            history.Add(RepeatQueryType.TweetsByHashtag, new QueryInfo());
            history.Add(RepeatQueryType.LocationsByHashtag, new QueryInfo());
        }

        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            string responseBody = await GetSearchResults(TweetSearchAPIUri(query, RepeatQueryType.TweetsByHashtag));
            if (responseBody == null)
                return null;
            var results = TweetSearchResults.FromJson(responseBody);
            LogQuery(query, results, RepeatQueryType.TweetsByHashtag);

            return results.Statuses;
        }

        [HttpGet("locations")]
        public async Task<IEnumerable<CountryResult>> Locations(string query)
        {
            string responseBody = await GetSearchResults(TweetSearchAPIUri(query, RepeatQueryType.LocationsByHashtag));
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

        private Uri TweetSearchAPIUri(string query, RepeatQueryType type)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/search/tweets.json")
            {
                Query = HashtagSearchQuery(query, type)
            };
            return bob.Uri;
        }

        private string HashtagSearchQuery(string query, RepeatQueryType type)
        {
            string result = $"q=%23{query}&count={TWEET_COUNT}&tweet_mode={TWEET_MODE}&include_entities={INCLUDE_ENTITIES}";
            if (query == history[type].LastQuery && history[type].LastMaxID != "")
                result += $"&max_id={history[type].LastMaxID}";
            return result;
        }

        private void AddBearerAuth(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {Configuration["bearerToken"]}");
        }
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

        private void LogQuery(string query, TweetSearchResults results, RepeatQueryType type)
        {
            history[type] = new QueryInfo(query, (long.Parse(results.Statuses.Min(status => status.IdStr)) - 1).ToString());
        }
    }
}
