using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public class ScraperHorry : Scraper
    {
        #region Overrides of Scraper

        public override bool CanScrape(string county)
        {
            return county == "South Carolina:Horry";
        }

        public override Scraper GetClone()
        {
            return new ScraperHorry();
        }

        public override Item Scrape(string parcelNumber)
        {
            ResetWebQuery();
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource("http://www.horrycounty.org/OnlineServices/LandRecords.aspx", 1);

            IDictionary<string, string> parameters = null;
            try
            {
                parameters = GetSearchParameters(doc, parcelNumber);
            }
            catch (Exception ex)
            {
                LogThrowErrorGettingParams(doc, ex);
            }

            InvokeSearching();
            doc = _webQuery.GetMultipartPost("http://www.horrycounty.org/OnlineServices/LandRecords.aspx", parameters);

            var item = GetItemFromSearchPage(doc);
            //            return item;
            item.MinimumBid = GetMinimumBid(parcelNumber);
            if (!item.MinimumBid.StartsWith("$") && !string.IsNullOrEmpty(item.MinimumBid))
            {
                item.MinimumBid = "$" + item.MinimumBid;
            }

            return item;
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            try
            {
                var value = GetValueOfFieldInDetailsPage(doc, "Residential Value");
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

        private string GetMinimumBid(string parcelNumber)
        {
            string minimumBidSearchUrl = "http://www.horrycounty.org:8080/EGSV2Horry/EPSearch.do";
            var doc = _webQuery.GetSource(minimumBidSearchUrl, 1);

            if (doc.DocumentNode.OuterHtml.Contains("eTax is not available from"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(doc.DocumentNode.OuterHtml, "(eTax is not available from [^<]+)");
                return match.Groups[1].Value;
            }

            var parameters = GetMinimumBidParameters(parcelNumber);

            doc = _webQuery.GetPost("http://www.horrycounty.org:8080/EGSV2Horry/EPResult.do", parameters, 1, minimumBidSearchUrl);

            if (doc.DocumentNode.OuterHtml.Contains("No results were found for your search criteria"))
            {
                return "";
            }

            //If more than one result appears
            if (doc.DocumentNode.OuterHtml.Contains("Search Results for Parcel"))
            {
                var nodes = doc.DocumentNode.SelectNodes("//table//tr/td[@class='SUBSCRIPTION'][8]");
                return WebQuery.Clean(nodes[nodes.Count - 1].InnerText);
            }
            return GetValueOfInput(doc, "amount");
        }

        private string GetMinimumBidParameters(string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("dispatch", "RecptSrch");
            dict.Add("srchlname", "");
            dict.Add("srchptype", "B");
            dict.Add("srchttype", "UNPAID");
            dict.Add("srchrecpt", "");
            dict.Add("srchppid", "");
            dict.Add("srchprcl", parcelNumber);
            dict.Add("srchstno", "");
            dict.Add("srchstnm", "");
            dict.Add("x", "46");
            dict.Add("y", "3");

            return WebQuery.GetStringFromParameters(dict);
        }

        private ItemWithMinimumBid GetItemFromSearchPage(HtmlDocument doc)
        {
            var rows = doc.DocumentNode.SelectNodes("//table[@id='dnn_ctr964_View_gvSearchResults']//tr");
            if (rows == null)
            {
                LogMessageCodeAndThrowException("Error getting rows from search result", doc.DocumentNode.OuterHtml);
            }
            if (doc.DocumentNode.OuterHtml.Contains("No results to display") || rows.Count < 2)
            {
                InvokeNoItemsFound();
                return null;
            }

            var row = rows[1];
            var cells = row.SelectNodes("./td");
            if (cells == null)
            {
                LogMessageCodeAndThrowException("No cells found in the 1st row", doc.DocumentNode.OuterHtml);
            }
            if (cells.Count != 9)
            {
                LogMessageCodeAndThrowException(string.Format("The number of cells was supposed to be 9 but was {0}", cells.Count), doc.DocumentNode.OuterHtml);
            }

            ItemWithMinimumBid item = new ItemWithMinimumBid();
            try
            {
                item.MapNumber = WebQuery.Clean(cells[0].InnerText);
                item.LegalDescription = WebQuery.Clean(cells[2].InnerText);
                AssignNames(item, WebQuery.Clean(cells[3].InnerText));
                item.OwnerAddress = WebQuery.Clean(cells[4].InnerText);
                item.OwnerCity = WebQuery.Clean(cells[5].InnerText);
                item.OwnerState = WebQuery.Clean(cells[6].InnerText);
                item.OwnerZip = WebQuery.Clean(cells[7].InnerText);
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }

            var parameters = GetDetailParameters(doc);
            doc = _webQuery.GetMultipartPost("http://www.horrycounty.org/OnlineServices/LandRecords.aspx", parameters);

            try
            {
                item.MarketValue = WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//span[@id='dnn_ctr964_View_lblFairMarket']").InnerText);
                item.LandValue = WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//span[@id='dnn_ctr964_View_lblResidential']").InnerText);

                if (item.LandValue != "0" && item.LandValue != "$0.00" && item.LandValue != "")
                {
                    item.OwnerResident = "Y";
                }
                else {
                    item.OwnerResident = "N";
                }
            }
            catch
            {

            }
            return item;
        }

        private static string GetValueOfFieldInDetailsPage(HtmlDocument doc, string fieldName)
        {
            var node = doc.DocumentNode.SelectSingleNode(string.Format("//td[contains(text(), '{0}')]", fieldName));
            string value = node.NextSibling.NextSibling.InnerText;
            return value;
        }

        private IDictionary<string, string> GetDetailParameters(HtmlDocument doc)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("__LASTFOCUS", "");
            dict.Add("StylesheetManager_TSSM", "");
            dict.Add("ScriptManager_TSM", "");
            dict.Add("__EVENTTARGET", "dnn$ctr964$View$gvSearchResults");
            dict.Add("__EVENTARGUMENT", "Select$0");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", GetValueOfInput(doc, "__VIEWSTATEENCRYPTED"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("dnn$ctl05$txtSearch", "");
            dict.Add("dnn$ctr964$View$txtSearch", GetValueOfInput(doc, "dnn$ctr964$View$txtSearch"));
            dict.Add("dnn$ctr1215$View$txtTMS", "");
            dict.Add("dnn$ctr1215$View$txtPIN", "");
            dict.Add("dnn$ctr992$UserKeySetter$UsernameChange", "");
            dict.Add("ScrollTop", "");
            dict.Add("__dnnVariable", GetValueOfInput(doc, "__dnnVariable"));
            dict.Add("____RequestVerificationToken", GetValueOfInput(doc, "__RequestVerificationToken"));
            return dict;
        }

        private IDictionary<string, string> GetSearchParameters(HtmlDocument doc, string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("__LASTFOCUS", "");
            dict.Add("StylesheetManager_TSSM", "");
            dict.Add("ScriptManager_TSM", "");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", GetValueOfInput(doc, "__VIEWSTATEENCRYPTED"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("dnn$ctl05$txtSearch", GetValueOfInput(doc, "dnn$ctl05$txtSearch"));
            dict.Add("dnn$ctr964$View$txtSearch", parcelNumber);
            dict.Add("dnn$ctr964$View$btnSearch", "Search");
            dict.Add("dnn$ctr1215$View$txtTMS", "");
            dict.Add("dnn$ctr1215$View$txtPIN", "");
            dict.Add("dnn$ctr992$UserKeySetter$UsernameChange", "");
            dict.Add("ScrollTop", "");
            dict.Add("__dnnVariable", GetValueOfInput(doc, "__dnnVariable"));
            dict.Add("__RequestVerificationToken", GetValueOfInput(doc, "__RequestVerificationToken"));
            return dict;
        }

        #endregion
    }
}
