using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers {
    class ScraperOrangeburg : Scraper {
        #region Overrides of Scraper

        private const string BaseUrl = "http://sc-orangeburg-assessor.governmax.com/propertymax";

        public override bool CanScrape(string county) {
            return county == "South Carolina:Orangeburg";
        }

        public override Scraper GetClone()
        {
            return new ScraperOrangeburg();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            var doc = _webQuery.GetSource("http://sc-orangeburg-assessor.governmax.com/propertymax/rover30.asp", 1);
            var s = doc.DocumentNode.SelectSingleNode("//frame[@src]");
            var sid = ParseSid(s.Attributes["src"].Value);

            InvokeOpeningSearchPage();
            doc =
                _webQuery.GetSource(
                    string.Format(
                        "http://sc-orangeburg-assessor.governmax.com/propertymax/search_property.asp?l_nm=taxacct&sid={0}",
                        sid), 1);

            InvokeSearching();
            string parameters = GetSearchParameters(sid, parcelNumber);
            doc = _webQuery.GetPost(
                "http://sc-orangeburg-assessor.governmax.com/propertymax/search_property.asp?go.x=1",
                parameters, 1);

            if (doc.DocumentNode.OuterHtml.Contains("No Records Found")) {
                InvokeNoItemsFound();
                return null;
            }

            Item item = new Item();
            try {
                item.MapNumber =
                    WebQuery.Clean(
                        doc.DocumentNode.SelectSingleNode(
                            "//table[@cellspacing='0' and @cellpadding='0' and @bordercolor='Lime']//tr[2]/td[1]").
                            InnerText);

                item.LegalDescription = GetValueOfField(doc, "Legal Desc.");
                item.Acreage = GetValueOfField(doc, "Legal Acreage");
                item.PhysicalAddress1 = WebQuery.Clean(
                    doc.DocumentNode.SelectSingleNode(
                        "//table[@cellspacing='0' and @cellpadding='0' and @bordercolor='Lime']//tr[2]/td[3]").InnerText);
                item.PhysicalAddress1 = item.PhysicalAddress1.TrimEnd(',');
                string name = GetValueOfField(doc, "Owner");
                AssignNames(item, name);
                var addressNode = GetNodeOfField(doc, "Owner Address");
                var textNodes = addressNode.SelectSingleNode("./font").Elements("#text").ToList();
                item.OwnerAddress = RemoveConsecutiveSpaces(WebQuery.Clean(textNodes[0].InnerText));
                string cityStateZip = RemoveConsecutiveSpaces(WebQuery.Clean(textNodes[1].InnerText));
                AssignCityStateZipToOwnerAddress(item, cityStateZip, "(.+) (.+) (.+)");
                item.MarketValue = GetMarketValue(doc);
            } catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            return item;
        }

        private double CleanNumber(string text) {
            string c = text.TrimStart('$').Replace(",", "").Replace(" ", "").Trim();
            var i = double.Parse(c);

            return i;
        }

        private string GetMarketValue(HtmlDocument doc) {
            var nodes = GetNodesOfField(doc, "Market Value");

            var texts = nodes.Select(x => WebQuery.Clean(x.InnerText)).ToList();

            string result = texts[0];
            double max = CleanNumber(texts[0]);

            foreach (var t in texts) {
                var number = CleanNumber(t);
                if (number >= max) {
                    result = t;
                    max = number;
                }
            }

            return result;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }

        private string GetSearchParameters(string sid, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("rm.taxacct", parcelNumber);
            dict.Add("go", "%A0%A0Go%A0%A0");
            dict.Add("site", "home");
            dict.Add("l_nm", "taxacct");
            dict.Add("sid", sid);
            return WebQuery.GetStringFromParameters(dict, false);
        }

        private string RemoveConsecutiveSpaces(string text) {
            return Regex.Replace(text, @"\s+", " ");
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName) {
            string result = WebQuery.Clean(GetNodeOfField(doc, fieldName).InnerText);
            return RemoveConsecutiveSpaces(result);
        }

        private List<HtmlNode> GetNodesOfField(HtmlDocument doc, string fieldName) {
            var nodes =
                doc.DocumentNode.SelectNodes(
                    string.Format("//td[@bgcolor='lightgrey']/font[@color='224119']/b[contains(text(), '{0}')]",
                                  fieldName));
            if (nodes == null)
            {
                LogMessageCodeAndThrowException(string.Format("Error getting field '{0}'", fieldName),
                                                doc.DocumentNode.OuterHtml);
            }

            return nodes.Where(x => WebQuery.Clean(x.InnerText) == fieldName).Select(x => x.ParentNode.ParentNode.NextSibling.NextSibling).ToList();
        }

        private HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName) {
            var node =
                doc.DocumentNode.SelectSingleNode(
                    string.Format("//td[@bgcolor='lightgrey']/font[@color='224119']/b[contains(text(), '{0}')]",
                                  fieldName));
            if (node == null) {
                LogMessageCodeAndThrowException(string.Format("Error getting field '{0}'", fieldName),
                                                doc.DocumentNode.OuterHtml);
            }

            return node.ParentNode.ParentNode.NextSibling.NextSibling;
        }

        private string ParseSid(string link) {
            var regex = Regex.Match(link, "sid=([^&]+)");
            if (!regex.Success) {
                LogMessageCodeAndThrowException(string.Format("Error getting SID code from link {0}", link));
            }
            return regex.Groups[1].Value;
        }

        #endregion
    }
}