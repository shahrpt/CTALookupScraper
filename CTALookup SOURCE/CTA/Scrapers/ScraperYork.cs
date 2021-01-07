using System;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public class ScraperYork : ScraperQpublic
    {
        public ScraperYork()
        {
            CountyCode = "sc_york";
            SearchUrl = "http://qpublic5.qpublic.net/sc_search.php?county=sc_york&search=parcel";
            SubmitUrl = "http://qpublic5.qpublic.net/sc_alsearch2.php";
            XpathLinkNodes = "//tr[@onmouseout and @onmouseover]";
        }

        #region Overrides of Scraper

        public override Scraper GetClone()
        {
            return new ScraperYork();
        }

        public override bool CanScrape(string county)
        {
            return county == "South Carolina:York";
        }

        public override Item Scrape(string parcelNumber)
        {
            var doc = Search(parcelNumber);
            if (NoRecordsFound(doc))
            {
                InvokeNoItemsFound();
                return null;
            }

            string link = GetLink(doc);

            return GetItem(link);

        }

        public override string GetOwnerName(HtmlDocument doc)
        {
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

        private Item GetItem(string link)
        {
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
                string cityStateZip;
                item.OwnerAddress = GetOwnerAddress(doc, out cityStateZip);
                AssignCityStateZipToOwnerAddress(item, cityStateZip);
                //item.MarketValue = 
                item.LandValue = GetValueFromPreliminaryValueTable(doc, 0);
                item.ImprovementValue = GetValueFromPreliminaryValueTable(doc, 1);
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }

            item.Images = GetImages(doc);
            return item;
        }

        #endregion

        private new string GetOwnerAddress(HtmlDocument doc, out string cityStateZip)
        {
            var node = GetNodeOfField(doc, "Mailing Address");
            var trNode = node.ParentNode;
            var tr2 = trNode.NextSibling.NextSibling;
            var tdNodes = tr2.SelectNodes("./td[@class='owner_value']");
            if (tdNodes.Count != 2)
            {
                LogMessageCodeAndThrowException("Error getting number of <TD> when obtaining owner address", doc.DocumentNode.OuterHtml);
            }

            string value = tdNodes[0].InnerText;
            string[] splitted = value.Split(new string[] { "&nbsp;" }, StringSplitOptions.RemoveEmptyEntries);
            if (splitted.Length != 2)
            {
                LogMessageCodeAndThrowException("Number of lines of Owner Address should be 2", doc.DocumentNode.OuterHtml);
            }
            cityStateZip = WebQuery.Clean(splitted[1]);
            return WebQuery.Clean(splitted[0]);
        }
    }
}
