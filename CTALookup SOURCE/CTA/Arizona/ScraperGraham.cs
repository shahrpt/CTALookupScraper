using System;
using System.Linq;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Arizona
{
    public class ScraperGraham : ScraperArizona
    {
        public override bool CanScrape(string county)
        {
            return county.ToLower() == "arizona:graham";
        }

        public override Scraper GetClone()
        {
            return new ScraperGraham();
        }

        public override Item Scrape(string parcelNumber)
        {
            string currentYear = DateTime.Today.Year.ToString();
            string url =
                string.Format(
                    "http://72.165.8.69/PropertyInfo/ParcelBase/HttpHandlers/AssessorInformation.ashx?apn={0}&taxyear={1}",
                    parcelNumber, currentYear);

            InvokeSearching();
            var doc = _webQuery.GetSource(url, 1);

            if (doc.DocumentNode.OuterHtml.Contains("Assessor information not found for tax year")) {
                return GetItemFromTaxInformation(parcelNumber);
            }

            return GetItem(doc);
        }

        private Item GetItemFromTaxInformation(string parcelNumber) {
            string url =
                string.Format("http://72.165.8.69/PropertyInfo/ParcelBase/HttpHandlers/TaxInformation.ashx?apn={0}",
                    parcelNumber);

            var doc = _webQuery.GetSource(url, 1);
            
            var item = new Item();
            try
            {
                var tableNode = doc.DocumentNode.SelectSingleNode("//div[contains(@id, 'Node')]/table");

                var n = tableNode.SelectSingleNode("./tr[2]");

                var texts = n.SelectNodes("./td/text()").Select(x => WebQuery.Clean(x.InnerText)).ToArray();

                //Name
                string name = texts[0];
                AssignNames(item, name);

                //Owner Address
                item.OwnerAddress = texts[1];

                //Owner City, State and Zip
                n = tableNode.SelectSingleNode("./tr[3]");
                string text = WebQuery.Clean(n.InnerText);
                try {
                    AssignCityStateZipToOwnerAddress(item, text, @"(.+),\s*([a-zA-Z]+)(.+)");
                }
                catch {}

                n = tableNode.SelectSingleNode("./tr[4]");

                item.LegalDescription = WebQuery.Clean(n.InnerText.Replace("Legal Description:", ""));
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }

            return item;
            
        }

        private Item GetItem(HtmlDocument doc)
        {
            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Parcel/Tax");
                item.LegalDescription = GetValueOfField(doc, "Legal Description");
                //item.Acreage = 
                item.PhysicalAddress1 = GetValueOfField(doc, "Site Address");
                item.PhysicalAddressState = "AZ";
                string name = GetValueOfField(doc, "Owner Name");
                AssignNames(item, name);

                item.OwnerAddress = GetValueOfField(doc, "Owner Address");

                string text = GetValueOfField(doc, "Owner City");
                try {
                    AssignCityStateZipToOwnerAddress(item, text, @"(.+),\s*([a-zA-Z]+)(.+)");
                }
                catch {}

                item.MarketValue = GetValueOfField(doc, "Full Cash Value");
                item.LandValue = GetValueOfField(doc, "Land Value");
                item.ImprovementValue = GetValueOfField(doc, "Improvement Value");
                item.Acreage = GetValueOfField(doc, "Parcel Size");
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName, bool ommit = true)
        {
            try
            {
                var n = doc.DocumentNode.SelectSingleNode(string.Format("//td[contains(., '{0}')]", fieldName));
                return WebQuery.Clean(n.Sibling("td").InnerText);
            }
            catch
            {
                if (ommit)
                {
                    return "n/a";
                }
                throw;
            }
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "Exempt Amount");
                return string.IsNullOrEmpty(value) || value == "$0.00" ? "N" : "Y";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }
    }
}
