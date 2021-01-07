namespace CTALookup.GoogleMaps
{
    public interface IAddressScraper
    {
        Address Scrape(string keyword);
    }
}