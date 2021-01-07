using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using CTALookup.Scrapers;

namespace CTALookup.Arizona
{
    public class ScraperMohave : ScraperApache
    {
        public ScraperMohave() {
            TableId = "table1";
        }

        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:mohave";
        }

        public override Scraper GetClone()
        {
            return new ScraperMohave();
        }

        public override Item Scrape(string parcelNumber) {
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource("http://legacy.co.mohave.az.us/depts/assessor/prop_info.asp", 1);

            string parameters = GetSearchParameters(parcelNumber);
            InvokeSearching();
            doc = _webQuery.GetPost("http://legacy.co.mohave.az.us/depts/assessor/prop_info.asp", parameters, 1);

            return GetItem(doc);
        }

        protected override Item GetItem(HtmlDocument doc) {
            var item = new Item();
            try {
                item.MapNumber = GetValueOfField(doc, "Parcel Number").Split(new string[] {"\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)[0];
                item.LegalDescription = GetValueOfField(doc, "Assessor Description");
                item.Acreage = GetValueOfField(doc, "Parcel Size").Replace(" Acres", "").Trim();
                
                string text = GetValueOfField(doc, "Site Address");
                Utils.AssignFullAddrToPhysicalAddress(item, text);
                item.PhysicalAddressState = "AZ";
                string name = GetValueOfField(doc, "Owner:");
                AssignNames(item, name);

                var addrNode = GetNodeOfField(doc, "Mailing Address");
                var texts =
                    addrNode.SelectNodes("./text()")
                        .Select(x => WebQuery.Clean(x.InnerText))
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                item.OwnerAddress = texts[0];
                AssignCityStateZipToOwnerAddress(item, texts[1], "(.+), (.+) (.+)");

                item.MarketValue = GetValueOfField(doc, "Full Cash Value");
                item.LandValue = GetValueOfField(doc, "Land Value");
                item.ImprovementValue = GetValueOfField(doc, "Improvement Value");
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private string GetSearchParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("PARCEL", parcelNumber);
            dict.Add("Year_Code", "CY");
            return WebQuery.GetStringFromParameters(dict);
        }
    }
}
