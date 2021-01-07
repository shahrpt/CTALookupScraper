using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperAnderson : Scraper
    {
        private const string SearchUrl = "http://acpass.andersoncountysc.org/real_prop_search.htm";
        private const string PostUrl = "http://acpass.andersoncountysc.org/asrmain.cgi";

        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Anderson";
        }

        public override Scraper GetClone()
        {
            return new ScraperAnderson();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            InvokeOpeningUrl(SearchUrl);
            var doc = _webQuery.GetSource(SearchUrl, 1);

            string parameters = "";
            try {
                parameters = GetSearchParameters(parcelNumber);
            } catch (Exception ex) {
                LogThrowErrorGettingParams(doc, ex);
            }

            InvokeSearching();
            doc = _webQuery.GetPost(PostUrl, parameters, 1);

            if (doc.DocumentNode.OuterHtml.Contains("Provided was not found")) {
                InvokeNoItemsFound();
                return null;
            }

            var item = new Item();
            try {
                item.MapNumber = WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//div[@align='center']/font[@size='2']/font[@color='#FF0000']/strong").InnerText);
                var legalDescNodes = GetNodeOfField(doc, "Legal Desc");
                if (legalDescNodes == null) {
                    LogMessageCodeAndThrowException("Error getting legal description nodes", doc.DocumentNode.OuterHtml);
                }
                string legalDesc = "";
                foreach (var node in legalDescNodes) {
                    legalDesc += " " + WebQuery.Clean(node.ParentNode.NextSibling.NextSibling.InnerText);
                }
                item.LegalDescription = legalDesc.Trim();
                item.Acreage =
                    WebQuery.Clean(
                        doc.DocumentNode.SelectSingleNode("//table[@height='379']//tr[1]//table[6]//tr[3]/td[2]").
                            InnerText);
                item.PhysicalAddress1 = GetValueOfField(doc, "Physical");
                string name = GetValueOfField(doc, "Name");
                AssignNames(item, name);
                item.OwnerAddress = GetValueOfField(doc, "Address");
                string cityState = GetValueOfField(doc, "City, State");
                var match = Regex.Match(cityState, "(.+) (.+)");
                if (!match.Success) {
                    LogMessageCodeAndThrowException(string.Format("Error getting city & state from text '{0}'", cityState), doc.DocumentNode.OuterHtml);
                }
                item.OwnerCity = match.Groups[1].Value.Trim();
                item.OwnerState = match.Groups[2].Value.Trim();
                item.OwnerZip = GetValueOfField(doc, "Zip");
                item.MarketValue = GetValueOfField(doc, "Market Value");
                item.OwnerResident = GetOwnerName(doc);
            } catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "Exempt");
                return value == "1" ? "Y" : "N";
            }
            catch (Exception ex) {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private static string GetValueOfField(HtmlDocument doc, string fieldName) {
            var nodes = GetNodeOfField(doc, fieldName);
            if (nodes == null) {
                LogMessageCodeAndThrowException(string.Format("Error getting value of field {0}", fieldName), doc.DocumentNode.OuterHtml);
            }
            return WebQuery.Clean(nodes[0].ParentNode.NextSibling.NextSibling.InnerText);
        }

        private static HtmlNodeCollection GetNodeOfField(HtmlDocument doc, string fieldName) {
            return doc.DocumentNode.SelectNodes(string.Format("//td[@bgcolor='#003300']//font[@color='#FFFFFF' and contains(text(), '{0}')]", fieldName));
        }


        private static string GetSearchParameters(string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("QryName", "");
            dict.Add("QryMapNo", parcelNumber);
            dict.Add("QryStreet", "");
            dict.Add("Sumbit.x", "46");
            dict.Add("Sumbit.y", "6");
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}
