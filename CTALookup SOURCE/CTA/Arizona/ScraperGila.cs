using CTALookup.Scrapers;
using System.Collections.Generic;
using System.Linq;

namespace CTALookup.Arizona
{
    public class ScraperGila : ScraperApache
    {
        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:gila";
        }

        public override Scraper GetClone()
        {
            return new ScraperGila();
        }

        public override Item Scrape(string parcelNumber) {
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource("https://parcelsearch.gilacountyaz.gov/", 1);

            string year = GetCheckedYear(doc);

            string searchUrl =
                string.Format(
                "https://parcelsearch.gilacountyaz.gov/parcelsearch.aspx?q={0}&tab=Tax%20Information&ty={1}&pn={0}",
                    parcelNumber, year);

            InvokeSearching();
            doc = _webQuery.GetSource(searchUrl, 1);

            var item = GetItem(doc);

            parcelNumber = parcelNumber.Replace("-", "");

            //Get Mailing Address from the other site
            doc = _webQuery.GetSource("http://taxes.gilacountyaz.gov/treasurer2/web/loginPOST.jsp?submit=Login&guest=true", 1);

            var parameters = GetMailingAddressParameters(parcelNumber);
            doc = _webQuery.GetPost("http://taxes.gilacountyaz.gov/treasurer2/treasurerweb/searchPOST.jsp", parameters, 1);

            if (doc.DocumentNode.OuterHtml.Contains("No accounts found"))
            {
                return item;
            }

            var aNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, 'account.jsp?account=')]");
            if (aNodes.Count > 2)
            {
                Logger.Log(string.Format("Mailing Address won't be obtained for parcel {0} because multiple results appeared", parcelNumber));
                return item;
            }

            string url = WebQuery.BuildUrl(WebQuery.Clean(aNodes[0].Attributes["href"].Value),
                "http://taxes.gilacountyaz.gov/treasurer2/treasurerweb/");
            doc = _webQuery.GetSource(url, 1);

            var tdNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'Address')]");
            if (tdNode == null)
            {
                Logger.Log(string.Format("No 'Address' field found for parcel {0}", parcelNumber));
                return item;
            }

            var td = tdNode.Sibling("td");

            var texts =
                td.SelectNodes("./text()")
                    .Select(x => WebQuery.Clean(x.InnerText))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

            item.MailingAddress = texts[0];

            AssignCityStateZipToMailingAddress(item, texts[1], @"(.+),\s*(.+)\s+(.+)");

            return item;
        }

        private string GetMailingAddressParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("TaxAccountID", "");
            dict.Add("TaxAOwnerIDSearchString", "");
            dict.Add("TaxAOwnerIDSearchType", "Standard Search");
            dict.Add("TaxAParcelID", parcelNumber);
            dict.Add("TaxASitusIDhouse", "");
            dict.Add("TaxASitusIDdirectionSuffix", "");
            dict.Add("TaxASitusIDstreet", "");
            dict.Add("TaxASitusIDdesignation", "");
            dict.Add("TaxASitusIDdirection", "");
            dict.Add("TaxASitusIDunit", "");
            return WebQuery.GetStringFromParameters(dict);
        }
    }
}
