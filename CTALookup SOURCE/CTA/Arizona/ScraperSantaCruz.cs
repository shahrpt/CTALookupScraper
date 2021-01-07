using CTALookup.Scrapers;
namespace CTALookup.Arizona
{
    public class ScraperSantaCruz : ScraperApache {
        public ScraperSantaCruz() {
            TableId = "search1_vwAssessor";
        }

        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:santa cruz";
        }

        public override Scraper GetClone()
        {
            return new ScraperSantaCruz();
        }

        public override Item Scrape(string parcelNumber) {
            InvokeOpeningUrl("http://parcelsearch.co.santa-cruz.az.us/parcelsearch.aspx");
            var doc = _webQuery.GetSource("http://parcelsearch.co.santa-cruz.az.us/parcelsearch.aspx", 1);

            string year = GetCheckedYear(doc);

            string searchUrl =
                string.Format(
                    "http://parcelsearch.co.santa-cruz.az.us/parcelsearch.aspx?q={0}&tab=Tax%20Information&ty={1}",
                    parcelNumber, year);

            InvokeSearching();
            doc = _webQuery.GetSource(searchUrl, 1);

            return GetItem(doc);
        }
    }
}
