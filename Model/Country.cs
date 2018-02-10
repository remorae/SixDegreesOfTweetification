namespace SixDegrees.Model
{
    public class Country
    {
        public string CountryName { get; }
        public City[] Cities { get; }

        public Country(string countryName, City[] cities)
        {
            CountryName = countryName;
            Cities = cities;
        }
    }
}
