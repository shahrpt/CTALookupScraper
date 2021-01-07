using System;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Arizona {
    public class ScraperNavajo : ScraperArizona {
        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:navajo";
        }

        public override Scraper GetClone()
        {
            return new ScraperNavajo();
        }

        public override Item Scrape(string parcelNumber) {
            string currentYear = DateTime.Today.Year.ToString();
            string url =
                string.Format(
                    "http://www.navajocountyaz.gov/pubworks/genii/parcels2/ParcelBase/HttpHandlers/AssessorInformation.ashx?apn={0}&taxyear={1}",
                    parcelNumber, currentYear);

            InvokeSearching();
            var doc = _webQuery.GetSource(url, 1);

            return GetItem(doc);
        }

        private Item GetItem(HtmlDocument doc) {
            var item = new Item();
            try {
                item.MapNumber = GetValueOfField(doc, "Parcel/Tax");
                item.LegalDescription = GetValueOfField(doc, "Legal Description");
                //item.Acreage = 
                item.PhysicalAddress1 = GetValueOfField(doc, "Site Address").TrimEnd(',', ' ');
                item.PhysicalAddressState = "AZ";
                string name = GetValueOfField(doc, "Owner Name").TrimEnd(',', ' ');
                AssignNames(item, name);

                item.OwnerFirstName = item.OwnerFirstName.TrimEnd('.');

                item.OwnerAddress = GetValueOfField(doc, "Owner Address");
                SplitLinealOwnerAddress(item, true);

                item.MarketValue = GetValueOfField(doc, "Full Cash Value");
                item.LandValue = GetValueOfField(doc, "Land Value");
                item.ImprovementValue = GetValueOfField(doc, "Improvement Value");
                item.Acreage = GetValueOfField(doc, "Parcel Size");
                item.OwnerResident = GetOwnerName(doc);

                Utils.RemoveDecimalValues(item);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName, bool ommit = true) {
            try {
                var n = doc.DocumentNode.SelectSingleNode(string.Format("//td[contains(., '{0}')]", fieldName));
                return WebQuery.Clean(n.Sibling("td").InnerText);
            }
            catch {
                if (ommit) {
                    return "n/a";
                }
                throw;
            }
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try {
                var value = GetValueOfField(doc, "Exempt Amount");
                return value == "0.00" ? "N" : "Y";
            }
            catch (Exception ex) {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }
    }
}