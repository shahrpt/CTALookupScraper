using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperRichland : Scraper
    {
        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Richland";
        }

        public override Scraper GetClone()
        {
            return new ScraperRichland();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            SetRequiredHeadersForThisSite();
            _webQuery.AllowAutoRedirect = false;
            var doc = _webQuery.GetSource("http://www4.rcgov.us/assessorsearchnew/AssessorSearch.aspx", 1);
            _webQuery.AllowAutoRedirect = true;

            string link = GetTrueLink(doc);
            //Visit the true link to obtain parameters
            doc = _webQuery.GetSource(link, 1);
            string parameters = GetParameters(doc, parcelNumber);

            InvokeSearching();
            doc = _webQuery.GetPost(link, parameters, 1, link);

            var rows = doc.DocumentNode.SelectNodes("//table[@id='DataGrid1']//tr");
            if (rows == null) {
                LogMessageCodeAndThrowException("Error getting rows", doc.DocumentNode.OuterHtml);
            }
            if (rows.Count == 1) {
                InvokeNoItemsFound();
                return null;
            }

            var row = rows[1];
            var cells = row.SelectNodes("./td");

            try
            {
                var detailsLink = WebQuery.Clean(cells[0].SelectSingleNode(".//a[@href]").Attributes["href"].Value);
                detailsLink = WebQuery.BuildUrl(detailsLink, GetBaseUrl(link));
                doc = _webQuery.GetSource(detailsLink, 1);
            }
            catch (Exception ex) {
                LogMessageCodeAndThrowException(string.Format("Error getting or opening details page in parcel {0}", parcelNumber), ex.Message);
            }

            var item = new Item();
            try
            {
                item.MapNumber = WebQuery.Clean(cells[1].InnerText);
                item.LegalDescription = GetValueOfInput(doc, "txtLegalDesc1");
                item.Acreage = GetValueOfInput(doc, "txtAcreage");
                try { item.PhysicalAddress1 = GetValueOfInput(doc, "txtPropLocation"); } catch { }
                //            item.PhysicalAddressCity = 
                //            item.PhysicalAddressState = 
                //            item.PhysicalAddressZip = 
                string name = GetValueOfInput(doc, "txtOwner");
                AssignNames(item, name);
                string city = GetValueOfInput(doc, "txtCity");
                string stateZip = GetValueOfInput(doc, "txtState");
                var cityStateZip = city + " " + stateZip;
                AssignCityStateZipToOwnerAddress(item, cityStateZip);
                item.OwnerAddress =
                    (GetValueOfInput(doc, "txtAddress1") + " " + GetValueOfInput(doc, "txtAddress2") + " " +
                     GetValueOfInput(doc, "txtAddress3")).Trim();
                item.LandValue = GetValueOfInput(doc, "txtLandValue");
                item.MarketValue = GetValueOfInput(doc, "txtMarketValue");
                item.ImprovementValue = GetValueOfInput(doc, "txtBuildValue");
                item.OwnerResident = GetOwnerName(doc);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfInput(doc, "txtHSFlag");
                return value == "No" ? "N" : "Y";
            }
            catch (Exception ex) {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        private static string GetBaseUrl(string link) {
            var match = Regex.Match(link, "(http://.*?/assessorsearchnew/.*?/)", RegexOptions.IgnoreCase);
            if (!match.Success) {
                LogMessageCodeAndThrowException(string.Format("Error getting base url from link {0}", link));
            }
            return match.Groups[1].Value;
        }

        private string GetTrueLink(HtmlDocument doc) {
            var linkNode = doc.DocumentNode.SelectSingleNode("//h2/a");
            var link = HttpUtility.UrlDecode(linkNode.Attributes["href"].Value);
            link = WebQuery.BuildUrl(link, "http://www4.rcgov.us");
            return link;
        }

        private void SetRequiredHeadersForThisSite() {
            _webQuery.Accept =
                "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
            _webQuery.UserAgent =
                "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; GTB0.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E; InfoPath.3)";
        }

        private string GetParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("__EVENTTARGET", "cmdSubmit1");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("txtLocation", "");
            dict.Add("TxtProperty", parcelNumber);
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}
