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

        private IConfiguration Configuration { get; }

        public SearchController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpGet("tweets")]
        public async Task<IEnumerable<Status>> Tweets(string query)
        {
            string responseBody = await GetSearchResults(TweetSearchAPIUri(query));
            if (responseBody == null)
                return null;
            TweetSearch results = JsonConvert.DeserializeObject<TweetSearch>(responseBody);
            return results.Statuses;
        }

        [HttpGet("locations")]
        public async Task<IEnumerable<CountryResult>> Locations(string query)
        {
            string responseBody = await GetSearchResults(TweetSearchAPIUri(query));
            if (responseBody == null)
                return null;
            TweetSearch results = JsonConvert.DeserializeObject<TweetSearch>(responseBody);
            Dictionary<string, Country> countries = new Dictionary<string, Country>();
            foreach (Status status in results.Statuses)
            {
                if (status.Place != null && status.Place.PlaceType == "city")
                    UpdateCountriesWithCity(countries, status);
                else if (status.Coordinates != null && status.Coordinates.Type == "Point")
                {
                    //TODO - Look up city/country names based on longitude/latitude
                }
            }
            return GetFormattedCountries(countries.Values);
        }

        private void UpdateCountriesWithCity(Dictionary<string, Country> countries, Status status)
        {
            string cityName = status.Place.Name;
            string countryName = status.Place.Country;
            if (!countries.ContainsKey(countryName))
                countries[countryName] = new Country(countryName);
            if (!countries[countryName].Cities.ContainsKey(cityName))
            {
                City toAdd = new City(cityName);
                countries[countryName].Cities[cityName] = toAdd;
            }
            foreach (Hashtag tag in status.Entities.Hashtags)
            {
                if (!countries[countryName].Cities[cityName].Hashtags.Contains(tag.Text))
                    countries[countryName].Cities[cityName].Hashtags.Add(tag.Text);
            }
        }

        private IEnumerable<CountryResult> GetFormattedCountries(IEnumerable<Country> countries)
        {
            return countries.Select(country =>
                new CountryResult(country.CountryName,
                country.Cities.Values.Select(city =>
                    new CityResult(city.CityName, city.Hashtags.AsEnumerable()))));
        }

        private Uri TweetSearchAPIUri(string query)
        {
            UriBuilder bob = new UriBuilder("https://api.twitter.com/1.1/search/tweets.json")
            {
                Query = StandardSearchQuery(query)
            };
            return bob.Uri;
        }

        private string StandardSearchQuery(string query)
        {
            return $"q={query}&count={TWEET_COUNT}&tweet_mode={TWEET_MODE}&include_entities={INCLUDE_ENTITIES}";
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

        private void AddBearerAuth(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization", $"Bearer {Configuration["bearerToken"]}");
        }
    }
}
