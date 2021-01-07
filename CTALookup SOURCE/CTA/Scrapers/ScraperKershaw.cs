using System;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public class ScraperKershaw : ScraperQpublic
    {
        public ScraperKershaw() {
            CountyCode = "sc_kershaw";
            SearchUrl = "http://qpublic5.qpublic.net/sc_search.php?county=sc_kershaw&search=parcel";
            SubmitUrl = "http://qpublic5.qpublic.net/sc_alsearch.php";
            XpathLinkNodes = "//tr[@onmouseout and @onmouseover]";
        }

        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Kershaw";
        }

        public override Scraper GetClone()
        {
            return new ScraperKershaw();
        }

        public override Item Scrape(string parcelNumber) {
            var doc = Search(parcelNumber);
            if (NoRecordsFound(doc)) {
                InvokeNoItemsFound();
                return null;
            }

            string link = GetLink(doc);

            return GetItem(link);

        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "Occupied");
                return value == "No" || string.IsNullOrEmpty(value) ? "N" : "Y";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private Item GetItem(string link) {
            var doc = _webQuery.GetSource(link, 1);

            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Parcel Number");
                item.LegalDescription = GetValueOfField(doc, "Description");
                item.Acreage = GetValueOfField(doc, "Acres");
                item.PhysicalAddress1 = GetValueOfField(doc, "Location Address");                
                string name = GetValueOfField(doc, "Owner Name");
                AssignNames(item, name);
                AssignOwnerAddress(item, doc);
                //item.MarketValue = 
                item.LandValue = GetValueFromPreliminaryValueTable(doc, 0);
                item.ImprovementValue = GetValueFromPreliminaryValueTable(doc, 1);
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        #endregion
    }
}
