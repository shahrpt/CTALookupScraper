using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers {
    internal class ScraperOconee : Scraper {
        private const string BaseUrl = "http://qpublic5.qpublic.net/ga_search_dw.php?county=sc_oconee&search=parcel";
        private const string PostUrl = "http://qpublic5.qpublic.net/ga_alsearch_dw.php";
        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Oconee";
        }

        public override Scraper GetClone()
        {
            return new ScraperOconee();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource(BaseUrl, 1);

            //var parameters = GetParameters(doc, parcelNumber);
            var parameters = GetLegalDescriptionParameters(doc, parcelNumber);
            InvokeSearching();
            doc = _webQuery.GetPost(PostUrl, parameters,1);
            HtmlNodeCollection tables = doc.DocumentNode.SelectNodes("//table[@class='table_class']");
          
            HtmlNodeCollection tds = tables[1].SelectNodes("//td[@class='search_value']");
           
            var linkNode = tds[0].FirstChild.NextSibling;
            string link = WebQuery.BuildUrl(linkNode.Attributes["href"].Value, BaseUrl);

            var item = GetItemFromPage(link,linkNode.InnerText);
            
            

            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try {
                var value =
                    WebQuery.Clean(
                        doc.DocumentNode.SelectSingleNode("//span[@id='FormView1_tax_residential_exemptionLabel']").
                            InnerText);
                return value == "0.00" ? "N" : "Y";
            }
            catch (Exception ex) {
                Logger.Log("Error getting Owner Resident from current doc");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private Item GetItemFromPage(string link,string map) {
            var doc = _webQuery.GetSource(link, 1);
            var item = new Item();
            try {
                HtmlNodeCollection values = doc.DocumentNode.SelectNodes("//td[@class='cell_value']");
                item.Acreage = values[9].InnerText.Replace("&nbsp;", "");
                string name = values[0].InnerText.Replace("&nbsp;", "").Trim();
                        
                AssignNames(item, name);
                item.OwnerAddress = values[2].InnerText.Replace("&nbsp;", "");
                string[] cityZip = values[4].InnerText.Replace("&nbsp;", "").Split(',');
                if (cityZip.Length > 0)
                {
                    item.OwnerCity = cityZip[0];
                }
                if (cityZip.Length > 1)
                {
                    string[] stateZip = cityZip[1].Trim().Split(' ');
                    if (stateZip.Length > 0)
                        item.OwnerState = stateZip[0];
                    if (stateZip.Length > 1)
                        item.OwnerZip = stateZip[1];
                }
                item.MapNumber = map;
                item.LegalDescription = values[8].InnerText.Replace("&nbsp;", "").Trim();

                
                item.LandValue = values[15].InnerText.Replace("&nbsp;", "").Trim().Replace("  ","");
                item.PhysicalAddress1 = values[6].InnerText.Replace("&nbsp;", "").Trim();
                item.ImprovementValue = values[16].InnerText.Replace("&nbsp;", "").Trim().Replace("  ", "");
                item.MarketValue = values[17].InnerText.Replace("&nbsp;", "").Trim().Replace("  ", "");
                
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            return item;
        }

        private T Inv<T>(Func<T> function, string errorMessage, string code = null) {
            try {
                return function();
            }
            catch (Exception ex) {
                Logger.LogMessageCodeAndThrowException(errorMessage, code, ex);
                //Patch
                throw;
            }
        }

        private string GetLegalDescription(string parcelNumber, out string detailsLink) {
            var doc = GetLegalDescriptionDoc(parcelNumber);
            return ParseLegalDescription(doc, out detailsLink);
        }

        private HtmlDocument GetLegalDescriptionDoc(string parcelNumber) {
            var doc = Inv(() => _webQuery.GetSource("http://taxpay.oconeesc.com/", 1),
                          "Error opening main url when obtaining legal description");
            string parameters = GetLegalDescriptionParameters(doc, parcelNumber);
            doc = Inv(() => _webQuery.GetPost("http://taxpay.oconeesc.com/", parameters, 1),
                      "Error submitting info when obtaining legal description");
            return doc;
        }

        public string ParseLegalDescription(HtmlDocument doc, out string detailsLink) {
            detailsLink = "";
            var rows = doc.DocumentNode.SelectNodes("//table[@id='GridView1']//tr[contains(@style, 'font-size:10px;')]");
            if (rows == null) {
                return "";
            }
            var orderedButNotFiltered = rows.OrderByDescending(x => WebQuery.Clean(x.SelectSingleNode("./td[6]").InnerText)).ToList();
            var ordered = orderedButNotFiltered.Where(x => LegalDescPredicate(x)).ToList();

            if (ordered.Count == 0) {
                detailsLink = Inv(() => ParseLink(orderedButNotFiltered[0]), "Error parsing link (orderedButNotFiltered) in current doc", doc.DocumentNode.OuterHtml);
                return "";
            }

            var result = WebQuery.Clean(ordered[0].SelectSingleNode("./td[4]").InnerText);

            detailsLink = Inv(() => ParseLink(ordered[0]), "Error parsing link (ordered) in current doc", doc.DocumentNode.OuterHtml);

            return result;
        }

        public string ParseLink(HtmlNode node) {
            var statusLink = node.SelectSingleNode("./td[7]//a");
            var href = Inv(() => WebQuery.Clean(statusLink.Attributes["href"].Value),
                                   "No link found");

            var m = Regex.Match(href, @"""(payment.aspx?[^""]+)");
            if (!m.Success)
            {
                Logger.LogMessageCodeAndThrowException(
                    string.Format("Error matching regex against href. HREF: {0}", href));
            }
            return WebQuery.BuildUrl(m.Groups[1].Value, "http://taxpay.oconeesc.com/");
        }

        private static bool LegalDescPredicate(HtmlNode cell) {
            var value = WebQuery.Clean(cell.SelectSingleNode("./td[4]").InnerText);
            return !string.IsNullOrEmpty(value) && !value.StartsWith("AD#");
        }

        private string GetLegalDescriptionParameters(HtmlDocument doc, string parcelNumber) {
            var splitted = parcelNumber.Split(new string[] {"-"}, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>();
            dict.Add("BEGIN", "0");
            dict.Add("INPUT", parcelNumber);
            dict.Add("searchType", "parcel_id");
            dict.Add("county", "sc_oconee");
            //dict.Add("__EVENTTARGET", "");
            //dict.Add("__EVENTARGUMENT", "");
            //dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            //dict.Add("__VIEWSTATEENCRYPTED", "");
            //dict.Add("__PREVIOUSPAGE", GetValueOfInput(doc, "__PREVIOUSPAGE"));
            //dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            //dict.Add("txtName", "");
            //dict.Add("txtReceipt", "");
            //dict.Add("txtReceipt1", "");
            //dict.Add("txtReceipt2", "");
            //dict.Add("txtMap1", splitted[0]);
            //dict.Add("txtMap2", splitted[1]);
            //dict.Add("txtMap3", splitted[2]);
            //dict.Add("txtMap4", splitted[3]);
            //dict.Add("txtMap", "");
            //dict.Add("Button2", "Search");
            //dict.Add("txtaddress", "");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("txtName", "");
            dict.Add("txtMap1", parcelNumber);
            dict.Add("Button2", "Search");
            dict.Add("txtaddress", "");
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__PREVIOUSPAGE", GetValueOfInput(doc, "__PREVIOUSPAGE"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}