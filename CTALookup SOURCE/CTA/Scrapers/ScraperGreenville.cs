using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperGreenville : Scraper
    {
        #region Overrides of Scraper
        public static int count = 0;
        public override bool CanScrape(string county) {
            return county == "South Carolina:Greenville";
        }

        public override Scraper GetClone()
        {
            return new ScraperGreenville();
        }

        public override Item Scrape(string parcelNumber) {

           
            var parameters = GetParameters(parcelNumber);
            InvokeSearching();
            var doc =
                _webQuery.GetPost(
                    "http://www.greenvillecounty.org/appsAS400/RealProperty/Details.aspx",
                    parameters, 1);

            
            if (!doc.DocumentNode.OuterHtml.Contains("Real Property Details")) {
                LogThrowNotLinkFound(doc);
                return null;
            }

            return GetItemFromPage(doc);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "HmstCd");
                return value == "*" ? "Y" : "N";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private Item GetItemFromPage(HtmlDocument doc) {
            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Map");
                item.LegalDescription = GetValueOfField(doc, "Desc");
                item.Acreage = GetValueOfField(doc, "Acreage");
                item.PhysicalAddress1 = Regex.Replace(GetValueOfField(doc, "Loc"), @"\s+", " ");
                string name = GetValueOfField(doc, "Owner");
                AssignNames(item, name);
                item.OwnerAddress = GetValueOfField(doc, "Mailing Address");
                
                SplitLinealOwnerAddress(item);
                item.MarketValue = GetValueOfField(doc, "Fair Market Value");
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        private string GetParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("MapNumber", parcelNumber);
            dict.Add("TaxYear", DateTime.Now.Year.ToString()); 
            //dict.Add("SelectYear", DateTime.Now.Year.ToString());
            //dict.Add("txt_Name", "");
            //dict.Add("txt_Street", "");
            //dict.Add("txt_MapNo", parcelNumber);
            //dict.Add("txt_Subdiv", "");
            //dict.Add("B1", "Submit");
            //dict.Add("txt_Voided_MApNo", "");
            //dict.Add("SelectSalesYear", "ALL");
            //dict.Add("txt_Sales_SheetNo", "");

            return WebQuery.GetStringFromParameters(dict);
        }

        private static string GetValueOfField(HtmlDocument doc, string fieldName) {
            string nnnoed = string.Format("//td[contains(@style, 'background-color') and contains(text(), '{0}')]", fieldName);
            HtmlNode table = doc.GetElementbyId("MyData");
            HtmlNode node = table.SelectSingleNode("//th[.//text()[contains(., '" + fieldName + "')]]/following-sibling::td");
          
            //var node = doc.DocumentNode.SelectSingleNode(string.Format("th[contains(text(), '{0}')]", fieldName+"#:"));
            //var node = doc.DocumentNode.SelectSingleNode(
            //    string.Format("//td[contains(@style, 'background-color') and contains(text(), '{0}')]", fieldName + "#:"));
            if (node == null) {
                //LogMessageCodeAndThrowException(string.Format("Error getting value of field {0}", fieldName), doc.DocumentNode.OuterHtml);
                node = new HtmlNode(HtmlNodeType.Element,doc,count);
                count++;
                node.InnerHtml = "N/A";
            }

            //return WebQuery.Clean(node.NextSibling.NextSibling.InnerText);
            return node.InnerText.ToString();
        }

        #endregion
    }
}
