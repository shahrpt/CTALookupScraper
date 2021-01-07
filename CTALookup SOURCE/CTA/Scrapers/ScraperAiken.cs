using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    //john todo
    //http://qpublic5.qpublic.net/ga_search_dw.php?county=sc_aiken
    //http://qpublic5.qpublic.net/ga_search_dw.php?county=sc_aiken&search=parcel
    public class ScraperAiken : Scraper {
        private const string Email = "trisoft998@yahoo.com";
        private const string Pass = "summer12";

        public override bool CanScrape(string county) {
            return county == "South Carolina:Aiken";
        }

        public override Scraper GetClone()
        {
            return new ScraperAiken();
        }

        private void SignIn() {
            var parameters = GetLoginParameters();

            InvokeNotifyEvent(string.Format("Signing in with email {0}", Email));
            var doc = _webQuery.GetPost("https://cxap2.aikencountysc.gov/EGSV2Aiken/LoginCS.do", parameters, 1,
                "https://cxap2.aikencountysc.gov/EGSV2Aiken/LoginCS.do");

            if (doc.DocumentNode.OuterHtml.Contains("Unknown User") ||
                doc.DocumentNode.OuterHtml.Contains("Incorret Password")) {
                LogMessageCodeAndThrowException(string.Format("Error signing in with email {0}", Email));
            }
        }

        public override Item Scrape(string parcelNumber) {
            var doc = _webQuery.GetSource("http://cxap2.aikencountysc.gov/EGSV2Aiken/CSSearch.do", 1);

            //Signing in if necessary
            if (doc.DocumentNode.OuterHtml.Contains("Login Required To Access")) {
                SignIn();
            }

            doc = _webQuery.GetSource("https://cxap2.aikencountysc.gov/EGSV2Aiken/RPSearch.do", 1);
            string parameters = GetSearchParameters(parcelNumber);
            InvokeSearching();
            doc = _webQuery.GetPost("https://cxap2.aikencountysc.gov/EGSV2Aiken/RPResult.do", parameters, 1);

            if (NoRecordsFound(doc))
            {
                InvokeNoItemsFound();
                return null;
            }

            return GetItem(doc, parcelNumber);
        }

        private bool NoRecordsFound(HtmlDocument doc) {
            return doc.DocumentNode.OuterHtml.Contains("Parcel Number not found");
        }

        private string GetSearchEtaxParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict = new Dictionary<string, string>();
            dict.Add("dispatch", "RecptSrch");
            dict.Add("srchrecpt", "");
            dict.Add("srchppid", "");
            dict.Add("srchprcl", parcelNumber);
            dict.Add("srchstno", "");
            dict.Add("srchstnm", "");
            dict.Add("x", "45");
            dict.Add("y", "3");
            dict.Add("srchlname", "");
            dict.Add("srchptype", "B");
            dict.Add("srchttype", "UNPAID");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetValue(HtmlDocument doc, string field) {
            var node = doc.DocumentNode.SelectSingleNode(string.Format("//b[contains(text(), '{0}')]", field));
            var text = node.ParentNode.ParentNode.NextSibling.NextSibling.SelectSingleNode(".//b").InnerText;

            return WebQuery.Clean(text);
        }

        private Item GetItem(HtmlDocument doc, string parcelNumber) {

            var item = new Item();
            item.MapNumber = GetValue(doc, "Parcel Number");
            item.Acreage = GetValue(doc, "Number of Acres");
            item.PhysicalAddress1 = GetValue(doc, "Property Location");
            item.MarketValue = GetValue(doc, "Total Appraised Value");
            
            string name = GetValue(doc, "Owner Name");
            AssignNames(item, name);

            item.LegalDescription = GetValue(doc, "Legal Description");
            
            InvokeNotifyEvent("Clicking on e-Tax Payments");
            doc = _webQuery.GetSource("http://cxap2.aikencountysc.gov/EGSV2Aiken/EPSearch.do", 1);

            InvokeSearching();
            string parameters = GetSearchEtaxParameters(parcelNumber);
            doc = _webQuery.GetPost("http://cxap2.aikencountysc.gov/EGSV2Aiken/EPResult.do", parameters, 1);

            if (doc.DocumentNode.OuterHtml.Contains("No results were found matching your search criteria")) {
                return item;
            }

            string[] addresses = GetAddress(doc);

            item.OwnerAddress = addresses[0];
            AssignCityStateZipToOwnerAddress(item, addresses[1], @"(.*)\s*,\s+(.*)\s+(.*)");
            item.Images = GetImages(doc);
            return item;
        }

        private string[] GetAddress(HtmlDocument doc) {
            var n = doc.DocumentNode.SelectSingleNode("//font[contains(text(), 'Address')]");
            var trNode = n.ParentNode.ParentNode.ParentNode.ParentNode.ParentNode;

            var addr1 = WebQuery.Clean(trNode.SelectSingleNode("./td[2]").InnerText);

            trNode = trNode.NextSibling.NextSibling.NextSibling.NextSibling;

            var addr2 = WebQuery.Clean(trNode.SelectSingleNode("./td[2]").InnerText);

            return new [] { addr1, addr2 };

        }

        private string GetEtaxValue(HtmlDocument doc, string field) {
            var node = doc.DocumentNode.SelectSingleNode(string.Format("//span[contains(text(), '{0}')]", field));
            node = node.ParentNode.ParentNode.ParentNode.NextSibling.NextSibling.SelectSingleNode(".//span");

            return WebQuery.Clean(node.InnerText);
        }

        private string GetSearchParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("dispatch", "PrclSrch");
            dict.Add("srchprcl", "");
            dict.Add("srchprclnew", parcelNumber);
            dict.Add("x", "41");
            dict.Add("y", "7");
            dict.Add("srchname", "");
            dict.Add("srchstno", "");
            dict.Add("srchstnm", "");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetLoginParameters() {
            var dict = new Dictionary<string, string>();
            dict.Add("username", Email);
            dict.Add("password", Pass);
            dict.Add("x", "25");
            dict.Add("y", "8");
            return WebQuery.GetStringFromParameters(dict);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }
    }
}
