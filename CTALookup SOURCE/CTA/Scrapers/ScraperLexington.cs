using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperLexington : Scraper
    {
        #region Overrides of Scraper

        private const string BaseUrl = "http://www.lex-co.com/scripts/cgiip.exe/WService=wsCAMA/CAMASearch/";

        public override bool CanScrape(string county) {
            return county == "South Carolina:Lexington";
        }

        public override Scraper GetClone()
        {
            return new ScraperLexington();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            InvokeSearching();
            string parameters = GetSearchParameters(parcelNumber);
            var doc = _webQuery.GetPost("http://www.lex-co.com/scripts/cgiip.exe/WService=wsCAMA/CAMASearch/dbsrch.htm",
                              parameters, 1);

            if (doc.DocumentNode.OuterHtml.Contains("Your search produced no results")) {
                InvokeNoItemsFound();
                return null;
            }

            var node = GetThisYearNode(doc);
            if (node == null) {
                LogMessageCodeAndThrowException(string.Format("Current year ({0}) not found in the list of results", DateTime.Now.Year), doc.DocumentNode.OuterHtml);
            }

            var rowLink = node.Element("td").SelectSingleNode("./a[@href]");
            string link = WebQuery.BuildUrl(rowLink.Attributes["href"].Value, BaseUrl);

            InvokeOpeningUrl(link);
            return GetItemFromPage(link);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "HOMESTEAD");
                return value == "0" || value == "" ? "N" : "Y";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private Item GetItemFromPage(string link) {
            var doc = _webQuery.GetSource(link, 1);

            var item = new Item();
            try {
                item.MapNumber = GetValueOfField(doc, "TMS#");
                item.LegalDescription = GetValueOfField(doc, "DESCRIPTION");
                item.Acreage = GetValueOfField(doc, "ACRES");
                item.PhysicalAddress1 = GetValueOfField(doc, "PROPERTY ADDRESS");
                string name = GetValueOfField(doc, "OWNER");
                AssignNames(item, name);
                string cityStateZip;
                item.OwnerAddress = GetOwnerAddress(doc, out cityStateZip);
                AssignCityStateZipToOwnerAddress(item, cityStateZip, "(.+), (.+) (.+)");
                item.LandValue = GetValueOfField(doc, "TAXABLE LAND");
                item.ImprovementValue = GetValueOfField(doc, "TAXABLE BUILDING");
                item.OwnerResident = GetOwnerName(doc);
            } catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        private string GetOwnerAddress(HtmlDocument doc, out string cityStateZip) {
            var node = GetNodeOfField(doc, "ADDRESS");
            string address = GetValueOfFieldFromNode(node);
            cityStateZip = node.ParentNode.ParentNode.NextSibling.NextSibling.SelectSingleNode("./td[2]").InnerText;
            
            //Check if the "ADDRESS" field has more than two rows
            var node2 = node.ParentNode.ParentNode.NextSibling.NextSibling.NextSibling.NextSibling;
            var text = WebQuery.Clean(node2.SelectSingleNode("./td[1]").InnerText);
                
            if (text == "") {
                address += " " + cityStateZip;
                cityStateZip = WebQuery.Clean(node2.SelectSingleNode("./td[2]").InnerText); ;
            }
            cityStateZip = cityStateZip.Replace("&nbsp;", " ").Replace(" ", " "); //Replacing char 160 by char 32.
            return address;
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName) {
            var node = GetNodeOfField(doc, fieldName);
            return GetValueOfFieldFromNode(node);
        }

        private string GetValueOfFieldFromNode(HtmlNode node) {
            var next = node.ParentNode.NextSibling;
            if (next.Name == "#text") {
                next = next.NextSibling;
            }
            return WebQuery.Clean(next.InnerText);
        }

        private HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName) {
            var node =
                doc.DocumentNode.SelectSingleNode(string.Format("//td[@align='RIGHT']/b[contains(text(), '{0}')]",
                                                                fieldName));
            if (node == null) {
                LogMessageCodeAndThrowException(string.Format("Error getting field {0}", fieldName),
                                                doc.DocumentNode.OuterHtml);
            }

            return node;
        }

        private HtmlNode GetThisYearNode(HtmlDocument doc) {
            var yearNodes = doc.DocumentNode.SelectNodes("//table[2]//tr/td");
            if (yearNodes.Count < 2) {
                LogMessageCodeAndThrowException("Error getting the table");
            }

            var node = yearNodes.Where(x => WebQuery.Clean(x.InnerText).Contains(DateTime.Now.Year.ToString())).FirstOrDefault();
            return node;
        }

        private static string GetSearchParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("TAXYR", "");
            dict.Add("TMS", parcelNumber);
            dict.Add("OWNER", "");
            dict.Add("ADDRESS", "");
            dict.Add("BOOK", "");
            dict.Add("PAGE", "");
            dict.Add("A1", "");
            dict.Add("ACRES1", "");
            dict.Add("B2", "");
            dict.Add("A2", "");
            dict.Add("ACRES2", "");
            dict.Add("LANDUSECODE", "");
            dict.Add("Order", "OWNER");
            dict.Add("Reposition", "0");
            dict.Add("Button", "Find");
            return WebQuery.GetStringFromParameters(dict);

        }

        #endregion
    }
}
