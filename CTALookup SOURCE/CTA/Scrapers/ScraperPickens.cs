using System;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperPickens : ScraperQpublic
    {
        #region Overrides of Scraper

        public ScraperPickens() {
            SearchUrl = "http://qpublic5.qpublic.net/psp/sc_pickens_parcel.php";
            SubmitUrl = "http://qpublic5.qpublic.net/sc_pickens_alsearch.php";
            XpathLinkNodes = "//tr[@onmouseover and @onmouseout]";
        }

        public override Scraper GetClone()
        {
            return new ScraperPickens();
        }

        public override bool CanScrape(string county) {
            return county == "South Carolina:Pickens";
        }

        public override Item Scrape(string parcelNumber) {
            var doc = Search(parcelNumber, false);
            if (NoRecordsFound(doc))
            {
                InvokeNoItemsFound();
                return null;
            }

            string link = GetLink(doc);
            InvokeOpeningUrl(link);
            Item item = null;
            try {
                item = GetItem(link);
            } catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueFromPreliminaryValueTable(doc, 3);
                return value.Contains("Not Legal Residence") ? "N" : "Y";
            }
            catch (Exception ex) {
                Logger.Log("Error getting Owner Resident from current doc");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private Item GetItem(string link) {
            var doc = _webQuery.GetSource(link, 1);
            var item = new Item();
            item.MapNumber = GetValueOfField(doc, "Parcel Number");
            item.LegalDescription = GetValueOfField(doc, "Legal Description");
            item.Acreage = GetValueOfField(doc, "Acres");
            item.PhysicalAddress1 = GetValueOfField(doc, "Location Address");
            string name = GetValueOfField(doc, "Owner Name");
            AssignNames(item, name);
            AssignOwnerAddress(item, doc);
            item.MarketValue = GetValueFromPreliminaryValueTable(doc, 0);
            item.OwnerResident = GetOwnerName(doc);
            item.Images = GetImages(doc);
            return item;
        }

        #endregion
    }
}
