using System;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public class ScraperFairfield : ScraperQpublic
    {
        public ScraperFairfield()
        {
            CountyCode = "sc_fairfield";
            SearchUrl = "http://qpublic5.qpublic.net/fl_search.php?county=sc_fairfield&searchType=parcel";
            SubmitUrl = "http://qpublic5.qpublic.net/sc_alsearch.php";
            XpathLinkNodes = "//table[@class='table_class']//tr[5]";
        }
        #region Overrides of Scraper


        public override bool CanScrape(string county)
        {
            return county == "South Carolina:Fairfield";
        }

        public override Scraper GetClone()
        {
            return new ScraperFairfield();
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

            var item = GetItem(link);
            //Use the same format as the input for Parcel
            item.MapNumber = parcelNumber;
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            try
            {
                var value = GetValueOfField(doc, "Homestead");
                return value == "Yes" ? "Y" : "N";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        public Item GetItem(string link)
        {
            var doc = _webQuery.GetSource(link, 1);
            Logger.Log(doc.DocumentNode.InnerHtml);
            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Parcel Number");
                item.LegalDescription = GetValueOfField(doc, "Legal Description");
                item.Acreage = GetValueOfField(doc, "Land Size");
                item.PhysicalAddress1 = GetValueOfField(doc, "Location Address");
                //                item.PhysicalAddressCity = 
                item.PhysicalAddressState = "SC";
                //                item.PhysicalAddressZip = 
                string name = GetValueOfField(doc, "Owner Name");
                AssignNames(item, name);
                AssignOwnerAddress(item, doc, Utils.RegexCityStateZip);
                item.LandValue = GetValue(doc, 2);
                item.ImprovementValue = GetValue(doc, 3);
                item.MarketValue = GetValue(doc, 5);
                item.OwnerResident = GetOwnerName(doc);

                var sq = GetVerticalValueFromQpublic(doc, "Sq Ft");
                var year = GetVerticalValueFromQpublic(doc, "Year Built");
                var ba = GetVerticalValueFromQpublic(doc, "Bathrooms");
                AssignNotes(item, sq, year, "", ba);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private string GetValue(HtmlDocument doc, int columnIndex)
        {
            var node = doc.DocumentNode.SelectSingleNode(string.Format("//center[3]/table[@class='table_class'][1]//tr[3]/td[{0}]",
                                                       columnIndex));
            if (node == null)
            {
                throw new Exception(string.Format("Error getting value of column index {0}", columnIndex));
            }
            string value = WebQuery.Clean(node.InnerText);
            return CleanValue(value);
        }

        #endregion
    }
}
