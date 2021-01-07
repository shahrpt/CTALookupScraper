using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperCharleston : Scraper
    {
        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Charleston";
        }

        public override Scraper GetClone()
        {
            return new ScraperCharleston();
        }

        private string ParseRedirect(HtmlDocument doc, string baseUrl = "http://sc-charleston-county.governmax.com/svc/collectmax/")
        {
            var m = Regex.Match(doc.DocumentNode.OuterHtml, @"location.replace\('(.+?)'");
            return baseUrl + m.Groups[1].Value;
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            string url = "http://sc-charleston-county.governmax.com/svc/agency/sc-charleston-county/homepage_new.asp";
            InvokeOpeningUrl(url);
            var doc = _webQuery.GetSource(url, 1);

            //Parsing 'Start your search' link
            var n = doc.DocumentNode.SelectSingleNode("//u[contains(text(), 'Start your search')]");
            n = n.Ancestors("a").First();
            string href = n.Attributes["href"].Value;

            href = href.Replace("../../", "");
            href = "http://sc-charleston-county.governmax.com/svc/" + href;

            InvokeNotifyEvent("Clicking on 'Start your search'");
            doc = _webQuery.GetSource(href, 1);

            url = ParseRedirect(doc);

            doc = _webQuery.GetSource(url, 1);

            //Click on "Property ID (PIN)"
            InvokeNotifyEvent("Clicking on 'Property ID (PIN)'");
            n = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Property ID (PIN)')]");
            href = n.Attributes["href"].Value;

            doc = _webQuery.GetSource(href, 1);

            url = ParseRedirect(doc, "http://sc-charleston-county.governmax.com/svc/propertymax/");

            doc = _webQuery.GetSource(url, 1, href);

            InvokeSearching();
            string parameters = GetSearchParameters(doc, parcelNumber);

            doc = _webQuery.GetPost("http://sc-charleston-county.governmax.com/svc/propertymax/search_property.asp?go.x=1",
                parameters, 1);

            url = ParseRedirect(doc, "http://sc-charleston-county.governmax.com/svc/propertymax/");

            doc = _webQuery.GetSource(url, 1);

            url = ParseRedirect(doc, "http://sc-charleston-county.governmax.com/svc/propertymax/Standard/");

            doc = _webQuery.GetSource(url, 1);

            if (doc.DocumentNode.OuterHtml.Contains("No Record Found")) {
                InvokeNoItemsFound();
                return null;
            }


            Item item = new Item();
            try {
                item.MapNumber = parcelNumber;
                item.LegalDescription = GetValueOfField(doc, "Legal");
                item.Acreage = GetValueOfField(doc, "Acreage");
                AssignPhysicalAddress(doc, item);
                item.PhysicalAddressState = "SC";
                string text = GetValueOfField(doc, "Owner");
                text = Regex.Replace(text, @"\s{4,}", " & ");

                AssignNames(item, text);

                AssignAddress(doc, item);

                AssignMarketLandAndImprovement(doc, item);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            return item;

        }

        private void AssignMarketLandAndImprovement(HtmlDocument doc, Item item) {
            var n = doc.DocumentNode.SelectSingleNode("//span[contains(text(), 'Historic Information')]");
            n = n.Ancestors("table").First();

            n = n.NextSibling;

            n = n.SelectSingleNode(".//tr[2]");

            item.LandValue = WebQuery.Clean(n.SelectSingleNode("./td[2]").InnerText);
            item.ImprovementValue = WebQuery.Clean(n.SelectSingleNode("./td[3]").InnerText);
            item.MarketValue = WebQuery.Clean(n.SelectSingleNode("./td[4]").InnerText);
        }

        private void AssignAddress(HtmlDocument doc, Item item) {
            var addrNode = GetNodeOfField(doc, "Owner Address");

            var texts = addrNode.SelectNodes(".//text()").Select(x => WebQuery.Clean(x.InnerText)).Where(x => !string.IsNullOrEmpty(x)).ToList();

            item.OwnerAddress = texts[0];

            AssignCityStateZipToOwnerAddress(item, texts[1]);
        }

        private void AssignPhysicalAddress(HtmlDocument doc, Item item) {
            var trNode =
                doc.DocumentNode.SelectSingleNode(
                    "//table[@width='100%' and @cellspacing='2' and @cellpadding='2']//tr[2]");
            var tdNode = trNode.SelectSingleNode("./td[3]");

            string text = WebQuery.Clean(tdNode.InnerText);

            var m = Regex.Match(text, @"(.+),\s*(.+)");
            item.PhysicalAddress1 = m.Groups[1].Value;
            item.PhysicalAddressCity = m.Groups[2].Value;
        }

        private HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName) {
            var n = doc.DocumentNode.SelectSingleNode(string.Format("//span[@class='datalabel' and contains(text(), '{0}')]", fieldName));
            return n.ParentNode.ParentNode.NextSibling.NextSibling;
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName) {
            var node = GetNodeOfField(doc, fieldName);
            if (node == null) {
                LogThrowErrorInField(doc, new Exception(string.Format("Error parsing node of field {0}", fieldName)));
            }

            return WebQuery.Clean(node.InnerText);
        }

        private string GetSearchParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("p.parcelid", parcelNumber);
            dict.Add("site", "propertysearch");
            dict.Add("l_nm", "parcelid");
            dict.Add("sid", ParseSid(doc));
            string parameters = WebQuery.GetStringFromParameters(dict);

            parameters += "&go=%A0%A0Go%A0%A0";

            return parameters;
        }

        private string ParseSid(HtmlDocument doc) {
            var m = Regex.Match(doc.DocumentNode.OuterHtml, "sid=(.+?)\"");
            return m.Groups[1].Value;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }

        private string GetAcreage(HtmlDocument doc) {
            var match = System.Text.RegularExpressions.Regex.Match(doc.DocumentNode.OuterHtml, @"High\s*:</b>.*?</font>([^<]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                LogMessageCodeAndThrowException("Error getting acreage", doc.DocumentNode.OuterHtml);
            }
            return Clean(match.Groups[1].Value);
        }

        private string GetOwnerNameText(HtmlDocument doc) {
            var node = doc.DocumentNode.SelectSingleNode("//table[@width='970'][3]//tr[2]/td[3]");
            if (node == null) {
                throw new Exception("Error getting Owner name");
            }
            return WebQuery.Clean(node.InnerText);
        }

        private string GetField(HtmlDocument doc, string fieldName) {
            var headerNode = GetFieldHeaderNode(doc, fieldName);
            var next = headerNode.NextSibling;
            if (next.Name != "#text") {
                return "";
            }
            return Clean(next.InnerText);
        }

        private static string Clean(string text) {
            return WebQuery.Clean(text).Replace("&nbsp", "").Replace((char)160, ' ');
        }

        private HtmlNode GetFieldHeaderNode(HtmlDocument doc, string fieldName) {
            var node = (from n in doc.DocumentNode.SelectNodes("//font[@size='3']")
                     where n.InnerText.Contains(fieldName)
                     select n).FirstOrDefault();
            if (node == null) {
                throw new Exception(string.Format("Error locating header {0}", fieldName));
            }
            return node;
        }

        private string GetParcelId(HtmlDocument doc) {
            var parcelNode = doc.DocumentNode.SelectSingleNode("//td[@width='250'][1]");
            if (parcelNode == null) {
                throw new Exception("Error getting parcel node");
            }
            return WebQuery.Clean(parcelNode.InnerText);
        }

        #endregion
    }
}
