using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System;
using System.Web;
using CTALookup.Scrapers;

namespace CTALookup.Georgia
{
    public class ScraperWhitfield : ScraperGeorgia
    {
        /*public ScraperWhitfield() : base("Whitfield", "ga_whitfield", "http://qpublic7.qpublic.net/ga_search.php?county=ga_whitfield&search=parcel") {}

        public override Item Scrape(string parcelNumber) {
            parcelNumber = parcelNumber.Replace("-", "");
            return base.Scrape(parcelNumber);
        }

        protected override IList<string> GetParcels(HtmlDocument doc) {
            var parcels = base.GetParcels(doc);

            return parcels.Select(x => x.Replace("-", "")).ToList();
        }*/

        public override bool CanScrape(string county)
        {
            return county == "Georgia:Whitfield";
        }

        public override Scraper GetClone()
        {
            return new ScraperWhitfield();
        }

        public ScraperWhitfield()
            : base("Whitfield", "ga_whitfield", "http://gis.whitfieldcountyga.com/GIS/Public/searchparcel_no.asp", "http://gis.whitfieldcountyga.com/GIS/PUBLIC/resultsparcel_no.asp?txtparcelno={0}&Submit=Search+Now")
        { }

        string GetValue(Func<string> func)
        {
            try
            {
                return func();
            }
            catch
            {
                return "n/a";
            }
        }

        public override Item Scrape(string parcelNumber)
        {
            _webQuery.ClearCookies();

            InvokeSearching();

            var parts = parcelNumber.Split('-').ToList();
            parts.RemoveAll(x => String.IsNullOrWhiteSpace(x));
            if (parts.Count != 4) throw new Exception("Wrong format");

            var doc = _webQuery.GetSource(SearchUrl, 1);

            doc = _webQuery.GetSource(String.Format(SubmitUrl, HttpUtility.UrlEncode(parcelNumber)), 1);
            if (doc.DocumentNode.OuterHtml.Contains("There are no records"))
            {
                InvokeNoItemsFound();
                return null;
            }
            var lnk = GetLink(doc);
            InvokeOpeningUrl(lnk);
            doc = _webQuery.GetSource(lnk, 1);

            var item = new Item();
            try
            {
                AssignFields(doc, item);
                item.MapNumber = parcelNumber;
                item.MailingState = "GA"; //some parcel ha another state. is it normal?
                AssignImage(doc, item);

            }
            catch (Exception ex) { }

            return item;
        }

        private HtmlNode GetTdByText(HtmlDocument doc, String Text, Boolean Sibling = false)
        {
            var res = doc.DocumentNode.SelectSingleNode(String.Format("//table/tr/td[contains(text(), '{0}')]", Text));
            if (res != null && Sibling) res = res.Sibling("td");
            return res;
        }

        private HtmlNode GetTrByTdText(HtmlDocument doc, String Text)
        {
            return doc.DocumentNode.SelectSingleNode(String.Format("//table/tr[contains(td/text(), '{0}')]", Text));
        }

        private HtmlNodeCollection GetTrCellsByTdText(HtmlDocument doc, String Text)
        {
            var res = doc.DocumentNode.SelectSingleNode(String.Format("//table/tr[contains(td/text(), '{0}')]", Text));
            return res.SelectNodes("td/text()");
        }

        private void AssignFields(HtmlDocument doc, Item item)
        {

            //var node = GetTrByTdText(doc, "Owner and Parcel");
            //if (node == null) return;
            //node = node.ParentNode;

            var node = GetTdByText(doc, "Owner Name", true);
            if (node != null) AssignNames(item, WebQuery.Clean(node.InnerText));
            item.OwnerAddress = GetValue(()=> WebQuery.Clean(GetTdByText(doc, "Owner Address", true).InnerText));
            var str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Owner Address 2", true).InnerText));
            if (str != "n/a") item.OwnerAddress2 = str;
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Owner Address 3", true).InnerText));
            if (str != "n/a") item.OwnerAddress2 += " "+str;
            item.OwnerCity = GetValue(()=> WebQuery.Clean(GetTdByText(doc, "Owner City", true).InnerText));
            item.OwnerState = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Owner State", true).InnerText));
            item.OwnerZip = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Owner Zip", true).InnerText));
            item.MapNumber = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Number", true).InnerText));

            item.LegalDescription = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Legal Description", true).InnerText));
            item.Acreage = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Total Acres", true).InnerText));

            item.ImprovementValue = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Residential Improvement", true).InnerText));
            item.LandValue = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Land", true).InnerText));
            item.MarketValue = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Current", true).InnerText));
            item.AccessoryValue = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Accessory Improvement", true).InnerText));
            item.HomesteadExcemption = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Homestead", true).InnerText));
            item.HomesteadExcemption = (String.IsNullOrEmpty(item.HomesteadExcemption) || item.HomesteadExcemption == "S0") ? "n" : "y";

            item.PhysicalAddress1 = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel House Number", true).InnerText));
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Street Extension", true).InnerText));
            if (!String.IsNullOrEmpty(str)) item.PhysicalAddress1 += " " + str;
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Street Direction", true).InnerText));
            if (!String.IsNullOrEmpty(str)) item.PhysicalAddress1 += " " + str;
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Street Name", true).InnerText));
            if (!String.IsNullOrEmpty(str)) item.PhysicalAddress1 += " " + str;
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Street Units", true).InnerText));
            if (!String.IsNullOrEmpty(str)) item.PhysicalAddress1 += " " + str;
            str = GetValue(() => WebQuery.Clean(GetTdByText(doc, "Parcel Street Type", true).InnerText));
            if (!String.IsNullOrEmpty(str)) item.PhysicalAddress1 += " " + str;
        }

        private void AssignLegalDesc(HtmlNode node, Item item)
        {
            var parts = node.SelectNodes("./tr/td/text()").Select(x => WebQuery.Clean(x.InnerText)).ToList();
            parts.RemoveAll(s => String.IsNullOrWhiteSpace(s) || s == "LEGAL DESC");
            item.LegalDescription = String.Join(" ", parts);
        }

        protected override string GetSearchOwnerUrl()
        {
            return "http://gis.whitfieldcountyga.com/GIS/Public/resultsparceldata.asp?lastname=b&address1=&Parceladdress=&legaldesc=&district=&landlot=&parcel=&subparcel=&mnuOrder=lastname&Submit=Search+Now";
        }

        protected override IList<string> GetParcels(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//table[2]/tr/td[last()-1]");
            if (nodes == null)
            {
                return new List<string> { "An error ocurred" };
            }
            return nodes.Skip(1).Select(x => WebQuery.Clean(x.InnerText)).ToList();
        }

        protected override string GetSearchOwnerParameters(string name)
        {
            return "";
        }

        protected override string GetLink(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//tr/td/a[text()='Parcel']");
            if (node == null)
            {
                LogThrowNotLinkFound(doc);
            }
            string link = WebQuery.BuildUrl(node.Attributes["href"].Value, BaseUrl);

            return link;
        }

        private void AssignImage(HtmlDocument doc, Item item)
        {
            var imgNode = doc.DocumentNode.SelectSingleNode("//tr/td/a/img");

            if (imgNode == null)
            {
                Logger.Log("The img node was null");
                Logger.LogCode(doc.DocumentNode.OuterHtml);
                return;
            }

            string src = imgNode.Attributes["src"].Value;

            if (src.EndsWith("NoPicture.jpg"))
            {
                return;
            }

            InvokeNotifyEvent("Getting property image");
            item.Image = _webQuery.GetImage(src, 1);
        }

    }
}
