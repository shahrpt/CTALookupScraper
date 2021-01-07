using CTALookup.Scrapers;
using System.Collections.Generic;
using System.Linq;

namespace CTALookup.Arizona
{
    public class ScraperYuma : ScraperTylerTechnologies
    {
        public ScraperYuma() : base("http://assessor.yumacountyaz.gov/") { }
        public override bool CanScrape(string county)
        {
            return county.ToLower() == "arizona:yuma";
        }

        public override Scraper GetClone()
        {
            return new ScraperYuma();
        }

        public override Item Scrape(string parcelNumber)
        {
            parcelNumber = parcelNumber.Replace("-", "");
            var item = base.Scrape(parcelNumber);
            if (item.Acreage == "n/a")
            {
                item.Acreage = "";
            }

            //Get Mailing Address

            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("TaxAccountID", "");
            dict.Add("TaxAOwnerIDSearchString", "");
            dict.Add("TaxAOwnerIDSearchType", "Standard Search");
            dict.Add("TaxAParcelID", parcelNumber);
            dict.Add("SitusAddressIDSearchString", "");
            dict.Add("SitusAddressIDSearchType", "Starts With");
            string parameters = WebQuery.GetStringFromParameters(dict);

            var doc =
                _webQuery.GetSource(
                    "http://treasurer.yumacountyaz.gov/treasurer/web/loginPOST.jsp?submit=Login&guest=true", 1);

            doc = _webQuery.GetPost("http://treasurer.yumacountyaz.gov/treasurer/treasurerweb/searchPOST.jsp",
                parameters, 1);

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
                "http://treasurer.yumacountyaz.gov/treasurer/treasurerweb/");
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
    }
}
