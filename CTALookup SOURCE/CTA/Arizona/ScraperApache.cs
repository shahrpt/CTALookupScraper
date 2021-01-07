using System;
using CTALookup.Scrapers;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace CTALookup.Arizona
{
    //john todo
    //changed website
    public class ScraperApache : ScraperArizona
    {
        protected string TableId;

        public ScraperApache()
        {
            TableId = "search1_vwAssessor";
        }
        public override bool CanScrape(string county)
        {
            return county.ToLower() == "arizona:apache";
        }

        public override Scraper GetClone()
        {
            return new ScraperApache();
        }

        protected string GetCheckedYear(HtmlDocument doc)
        {
            var inputNode = doc.DocumentNode.SelectSingleNode("//input[contains(@id, 'AssessYear') and @checked]");
            if (inputNode == null)
            {
                LogMessageCodeAndThrowException("Error parsing year in current document", doc.DocumentNode.OuterHtml);
            }

            return
                WebQuery.Clean(
                    doc.DocumentNode.SelectSingleNode(string.Format("//label[@for='{0}']",
                        inputNode.Attributes["id"].Value)).InnerText);
        }

        protected string GetValueOfField(HtmlDocument doc, string fieldName, bool ommit = true)
        {
            try
            {
                var next = GetNodeOfField(doc, fieldName);

                return WebQuery.Clean(next.InnerText);
            }
            catch (Exception e)
            {
                if (ommit)
                {
                    return "n/a";
                }
                throw;
            }
        }

        protected virtual HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            var node =
                doc.DocumentNode.SelectSingleNode(
                    string.Format("//table[@id='{0}']//tr/td[1][contains(text(), '{1}')]", TableId, fieldName));

            var next = node.Sibling("td");
            return next;
        }

        public void PostAgree(HtmlDocument doc)
        {
            var posturl = "http://eagleweb.treasurer.co.apache.az.us:8080/treasurer/web/loginPOST.jsp";
            var dict = new Dictionary<string, string>();
            dict.Add("guest", "true");
            dict.Add("submit", "I Agree to the Above Statement");
            var postparams = WebQuery.GetStringFromParameters(dict);
            var dd = _webQuery.GetPost(posturl, postparams, 1);
            doc = dd;
        }

        public override Item Scrape(string parcelNumber)
        {
            InvokeOpeningUrl("www.co.apache.az.us/parcelsearch/");


            var doc = _webQuery.GetSource("http://www.co.apache.az.us/parcelsearch/", 1);
            PostAgree(doc);

            string year = GetCheckedYear(doc);

            string searchUrl =
                string.Format(
                    "http://www.co.apache.az.us/parcelsearch/ParcelSearch.aspx?q={0}&tab=Tax%20Information&ty={1}",
                    parcelNumber, year);

            InvokeSearching();
            doc = _webQuery.GetSource(searchUrl, 1);

            return GetItem(doc);
        }

        protected virtual Item GetItem(HtmlDocument doc)
        {
            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Parcel/Tax");
                item.LegalDescription = GetValueOfField(doc, "Legal Description");
                //item.Acreage = 
                AssignPhysicalAddress(doc, item);

                //                item.PhysicalAddressCity = 
                item.PhysicalAddressState = "AZ";
                //item.PhysicalAddressZip = 
                string name = GetValueOfField(doc, "Owner Name").TrimEnd(',', ' ');
                AssignNames(item, name);

                item.OwnerAddress = GetValueOfField(doc, "Owner Address");
                SplitLinealOwnerAddress(item, true);

                item.MarketValue = GetValueOfField(doc, "Full Cash Value");
                item.LandValue = GetValueOfField(doc, "Land Value");
                item.ImprovementValue = GetValueOfField(doc, "Improvement Value");
                item.OwnerResident = GetOwnerName(doc);

                Utils.RemoveDecimalValues(item);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private void AssignPhysicalAddress(HtmlDocument doc, Item item)
        {
            string text = GetValueOfField(doc, "Site Address").TrimEnd(',', ' ');

            if (text.Contains(","))
            {
                var m = System.Text.RegularExpressions.Regex.Match(text, @"(.+),\s*(.+)");

                item.PhysicalAddress1 = m.Groups[1].Value;
                item.PhysicalAddressCity = m.Groups[2].Value;
            }
            else
            {
                item.PhysicalAddress1 = text;
            }
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            try
            {
                var value = GetValueOfField(doc, "Exempt Amount");
                return value == "$0.00" ? "N" : "Y";
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