using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperGreenwood : Scraper {
        //private const string SearchUrl = "http://co.greenwood.sc.us/tax/Default.aspx";
        private const string SearchUrl = "http://www.greenwoodsc.gov/Property_Report/Default.aspx";
        
        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Greenwood";
        }

        public override Scraper GetClone()
        {
            return new ScraperGreenwood();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();

            var doc = new HtmlDocument();
            string parameters = GetParameters(parcelNumber, "", "");

            InvokeSearching();
            WebRequest request = WebRequest.Create(SearchUrl+"?"+parameters);
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            doc.LoadHtml(responseFromServer);

            InvokeSearching();

            if (doc.DocumentNode.OuterHtml.Contains("No Records Found")) {
                InvokeNoItemsFound();
                return null;
            }
            if (!(doc.DocumentNode.InnerText.Contains("Owner Information")))
            {
                InvokeNoItemsFound();
                return null;
            }
            Item item = null;
            try {
                item = GetItemFromSearchResults(doc, parcelNumber);
            } catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }

        private Item GetItemFromSearchResults(HtmlDocument doc, string parcelNumber) {
            //var rowsNodes = doc.DocumentNode.SelectNodes("//tr[@class='gvRow']");
            //if (rowsNodes == null) {
            //    LogMessageCodeAndThrowException("Error getting rows", doc.DocumentNode.OuterHtml);
            //}
            //var cells = rowsNodes[0].SelectNodes("./td");
            HtmlNodeCollection tds = doc.DocumentNode.SelectNodes("//td");

            var item = new Item();
            item.MapNumber = tds[0].InnerText.ToString();
            //string parameters = GetViewParameters(doc, parcelNumber);
            //string parameters = "";
            InvokeNotifyEvent("Opening details page");

            //doc = _webQuery.GetPost(SearchUrl, parameters, 1);

            item.LegalDescription = tds[2].InnerText;
            item.PhysicalAddress1 = tds[1].InnerText;
            string name = tds[5].InnerText.ToString();
            AssignNames(item, name);

            //var addressNode = doc.DocumentNode.SelectSingleNode("//span[@id='Greenwood_Tax1_lblAddr1']");
            //if (addressNode == null) {
            //    throw new Exception("Error getting address node");
            //}

            //var splitted = addressNode.Elements("#text").ToList();

            item.OwnerAddress = tds[7].InnerText.ToString();
         
            //if (splitted.Count > 2)
            //{
            //    item.OwnerAddress += " " + WebQuery.Clean(splitted[1].InnerText);
            //    GetCityAndState(WebQuery.Clean(splitted[2].InnerText), out city, out state);
            //}
            //else {
            //    GetCityAndState(WebQuery.Clean(splitted[1].InnerText), out city, out state);
            //}
            List<string> cityandState = tds[9].InnerText.Split(',').ToList();
            if (cityandState.Count > 0)
            {
                item.OwnerCity = cityandState[0].ToString();
                string[] stZip = cityandState[1].Trim().Split(' ');
                if (stZip.Length == 2)
                {
                    item.OwnerState = stZip[0];
                    item.OwnerZip = stZip[1];
                }
            }

            item.ImprovementValue = GetValueOfVerticalField(doc, "Fair Market Value - Improv");
            item.LandValue = GetValueOfVerticalField(doc, "Fair Market Value - Land");
            item.MarketValue = GetValueOfVerticalField(doc, "Fair Market Value - Total");

            return item;
        }

        private string GetValueOfVerticalField(HtmlDocument doc, string fieldName) {
            var th = doc.DocumentNode.SelectSingleNode(string.Format("//th[contains(text(), '{0}')]", fieldName));
            var n = th.Ancestors("thead").First();

            var index = n.SelectNodes("./tr/th").Where(x => !x.GetAttributeValue("style", "").StartsWith("padding:")).ToList().IndexOf(th);

            var trNodes = n.NextSibling.SelectNodes("./tr/td").ToList();

            n = trNodes[index];

            return WebQuery.Clean(n.InnerText);
        }

        private void GetCityAndState(string text, out string city, out string state) {
            var match = Regex.Match(text, "(.*) (.*)");
            if (!match.Success) {
                LogMessageCodeAndThrowException(string.Format("Error getting city and state from '{0}'", text));
            }
            city = match.Groups[1].Value.Trim();
            state = match.Groups[2].Value.Trim();
        }

        private string GetViewParameters(string parcelNumber) {
            return GetParameters(parcelNumber, "Greenwood_Tax1$gvRecordsList$ctl02$btnView", "View");
        }

        private string GetSearchParameters(string parcelNumber) {
            return "";
        }

        private string GetParameters(string parcelNumber, string btnSearchName, string btnSearchValue) {
            var dict = new Dictionary<string, string>();
            dict.Add("pin", parcelNumber);
            dict.Add("isTinyScreen", "false");
            //dict.Add("__EVENTTARGET", "");
            //dict.Add("__EVENTARGUMENT", "");
            //dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            //dict.Add("__VIEWSTATEENCRYPTED", "");
            //dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            //dict.Add("Greenwood_Tax1$rblRecType", "Property");
            //dict.Add("Greenwood_Tax1$drpYear", "All");
            //dict.Add("Greenwood_Tax1$drpStatus", "Both");
            //dict.Add("Greenwood_Tax1$drpSearchParam", "Map Number");
            //dict.Add("Greenwood_Tax1$txtSearchParam", parcelNumber);
            //dict.Add(btnSearchName, btnSearchValue);
            //dict.Add("Greenwood_Tax1$txtInsured", "");
            //dict.Add("Greenwood_Tax1$txtLastName", "");
            //dict.Add("Greenwood_Tax1$txtFirstName", "");
            //dict.Add("Greenwood_Tax1$txtDayPhone", "");
            //dict.Add("Greenwood_Tax1$meDayPhone_ClientState", "");
            //dict.Add("Greenwood_Tax1$txtEmail", "");
            //dict.Add("Greenwood_Tax1$txtAddress", "");
            //dict.Add("Greenwood_Tax1$txtCity", "");
            //dict.Add("Greenwood_Tax1$drpState", "AL");
            //dict.Add("Greenwood_Tax1$txtZip", "");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetAcceptAgreementsParameters(HtmlDocument doc) {
            var dict = new Dictionary<string, string>();
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__PREVIOUSPAGE", GetValueOfInput(doc, "__PREVIOUSPAGE"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("Greenwood_Tax1$btnAccept", "Yes, I do accept");
            dict.Add("Greenwood_Tax1$txtInsured", "");
            dict.Add("Greenwood_Tax1$txtLastName", "");
            dict.Add("Greenwood_Tax1$txtFirstName", "");
            dict.Add("Greenwood_Tax1$txtDayPhone", "");
            dict.Add("Greenwood_Tax1$meDayPhone_ClientState", "");
            dict.Add("Greenwood_Tax1$txtEmail", "");
            dict.Add("Greenwood_Tax1$txtAddress", "");
            dict.Add("Greenwood_Tax1$txtCity", "");
            dict.Add("Greenwood_Tax1$drpState", "AL");
            dict.Add("Greenwood_Tax1$txtZip", "");
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}
