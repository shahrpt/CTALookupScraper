using HtmlAgilityPack;
using System.Collections.Generic;
using System;
using System.Linq;
using CTALookup.Scrapers;

namespace CTALookup.Georgia
{
    public class ScraperHenry : ScraperGeorgia
    {

        public ScraperHenry()
            : base("Henry", "ga_henry", "http://qpublic7.qpublic.net/ga_henry_search.php?county=ga_henry&search=parcel",
            "http://qpublic7.qpublic.net/ga_henry_alsearch.php") {

            /*: base("Henry", "ga_henry", "http://www.qpublic.net/ga/henry/parcel.html",
            "http://qpublic3.qpublic.net/ga_henry_alsearch.php",
            "", 
            "http://qpublic3.qpublic.net/") {*/
        }

        public override Scraper GetClone()
        {
            return new ScraperHenry();
        }

        /*protected override string GetSearchOwnerUrl() {
            return "http://www.qpublic.net/ga/henry/name.html";
        }*/

        /*protected override string GetParameters(string parcelNumber, bool addCountyCode = true) {
            var dict = new Dictionary<string, string>();
            dict.Add("BEGIN", "0");
            dict.Add("INPUT", parcelNumber);
            dict.Add("searchType", "parcel_number");
            dict.Add("county", "henry");
            dict.Add("state", "ga");
            dict.Add("Parcel Search", "Search By Parcel");
            return WebQuery.GetStringFromParameters(dict);
        }*/

        /*protected override string GetSearchOwnerParameters(string name) {
            var dict = new Dictionary<string, string>();
            dict.Add("BEGIN", "0");
            dict.Add("INPUT", name);
            dict.Add("searchType", "owner_name");
            dict.Add("county", "henry");
            dict.Add("state", "ga");
            dict.Add("Owner Search", "Search By Owner Name");
            return WebQuery.GetStringFromParameters(dict);
        }*/

        protected override IList<string> GetParcels(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//td[@class='celll_value'][1]/a");
            if (nodes == null)
            {
                return new List<string> { "An error ocurred" };
            }

            return nodes.Select(x => WebQuery.Clean(x.InnerText)).ToList();
        }

        protected override string GetLink(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//td[@class='celll_value'][1]/a");
            if (node == null)
            {
                LogThrowNotLinkFound(doc);
            }
            string link = WebQuery.BuildUrl(node.Attributes["href"].Value, BaseUrl);

            return link;
        }

        /*public override Item Scrape(string parcelNumber)
        {
            var doc = Search(parcelNumber);
            if (NoRecordsFound(doc))
            {
                InvokeNoItemsFound();
                return null;
            }

            string link = GetLink(doc);
            InvokeOpeningUrl(link);
            Item item = null;
            try
            {
                item = GetItem(link);
                if (item.MapNumber == "n/a")
                {
                    item.MapNumber = parcelNumber;
                }
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }*/

        /*private Item GetItem(string link)
        {
            var doc = _webQuery.GetSource(link, 1);

            var nextLinkNode =
                doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Click Here To Continue To Property')]");
            if (nextLinkNode != null)
            {
                string url = WebQuery.BuildUrl(WebQuery.Clean(nextLinkNode.Attributes["href"].Value), BaseUrl);
                doc = _webQuery.GetSource(url, 1);
            }

            var item = new Item();
            item.MapNumber = GetValueOfField(doc, "Parcel Number");
            //item.LegalDescription = GetValueOfField(doc, "Legal Description");
            item.Acreage = GetValueOfField(doc, "Total Acres");//Acres 
            item.PhysicalAddress1 = GetValueOfField(doc, "Location Address");
            string name = GetValueOfField(doc, "Owner Name");
            AssignNames(item, name);

            AssignOwnerAddress(item, doc);

            item.MarketValue = GetVerticalValueFromQpublic(doc, "Total Value", "Market Value");
            item.LandValue = GetVerticalValueFromQpublic(doc, "Land Value");
            item.AccessoryValue = GetVerticalValueFromQpublic(doc, "Accessory Value");
            item.ImprovementValue = GetVerticalValueFromQpublic(doc, "Improvement Value", "Building Value");
            item.OwnerResident = GetOwnerResident(doc);
            return item;
        }*/
    }
}
