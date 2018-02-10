namespace SixDegrees.Model
{
    public class City
    {
        public string CityName { get; }
        public string[] Hashtags { get; }

        public City(string cityName, string[] hashtags)
        {
            CityName = cityName;
            Hashtags = hashtags;
        }
    }
}
