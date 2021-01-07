using CTALookup.Scrapers;
namespace CTALookup.Arizona {
    public class ScraperLaPaz : ScraperTylerTechnologies {
        public ScraperLaPaz() : base("http://24.121.225.10/") {}

        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:la paz";
        }

        public override Scraper GetClone()
        {
            return new ScraperLaPaz();
        }

        public override Item Scrape(string parcelNumber) {
            var item = base.Scrape(parcelNumber);
            if (item.Acreage == "n/a") {
                item.Acreage = "";
            }
            return item;
        }
    }
}