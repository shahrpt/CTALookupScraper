using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperLaurens : Scraper
    {
        private const string SearchUrl = "http://laurenscountysctaxes.com/secondary.aspx?pageID=175";
        private bool _firstTime;
        #region Overrides of Scraper

        public ScraperLaurens() {
            _firstTime = true;
        }

        public override Scraper GetClone()
        {
            return new ScraperLaurens();
        }

        public override bool CanScrape(string county) {
            return county == "South Carolina:Laurens";
        }

        public override Item Scrape(string parcelNumber) {
            if (_firstTime) {
                ResetWebQuery();
            }
            _firstTime = false;
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource(SearchUrl, 1);
            if (doc.DocumentNode.OuterHtml.Contains("No Records Found")) {
                InvokeNoItemsFound();
                return null;
            }
            InvokeNotifyEvent("Accepting agreement");
            doc = AcceptAgreementIfNecessary(doc);

            string parameters = null;
            try
            {
                parameters = GetSearchParameters(doc, parcelNumber);
            }
            catch (Exception ex) {
                LogThrowErrorGettingParams(doc, ex);
            }

            InvokeSearching();
            doc = _webQuery.GetPost(SearchUrl, parameters, 1);
            
            var item = new Item();

            try
            {
                item.MapNumber = GetValueOfCell(doc, 1);
                item.LegalDescription = GetValueOfCell(doc, 3);
                string name = GetValueOfCell(doc, 2);
                AssignNames(item, name);

                //Opening 'View'
                try { 
                    parameters = GetViewParameters(doc, item.MapNumber);
                }
                catch (Exception ex)
                {
                    LogMessageCodeAndThrowException("Error getting 'View' parameters", doc.DocumentNode.OuterHtml, ex);
                }
                InvokeNotifyEvent("Opening details page");
                doc = _webQuery.GetPost(SearchUrl, parameters, 1);

                item.PhysicalAddress1 = GetInnerText(doc, "span", "id", "ctl00_Main_SkeletonCtrl_160_lblPhysicalAddr");
                item.OwnerAddress = GetInnerText(doc, "span", "id", "ctl00_Main_SkeletonCtrl_160_lblAddr1");
                string cityStateZip = GetInnerText(doc, "span", "id", "ctl00_Main_SkeletonCtrl_160_lblCityStateZip");
                AssignCityStateZipToOwnerAddress(item, cityStateZip, "(.+) (.+) (.+)");
                //Fixing some page problem. Sometimes appears "S C" instead of "SC"
                FixZipStateSpaceIssue(item);

                item.LandValue = GetInnerText(doc, "span", "id", "ctl00_Main_SkeletonCtrl_160_landApr1");
                item.ImprovementValue = GetInnerText(doc, "span", "id", "ctl00_Main_SkeletonCtrl_160_buildApr1");
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }

        private void FixZipStateSpaceIssue(Item item) {
            if (item.OwnerCity.EndsWith(" S") && item.OwnerState == "C") {
                item.OwnerCity = item.OwnerCity.Substring(0, item.OwnerCity.Length - 2);
                item.OwnerState = "SC";
            }
        }

        private string GetViewParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            AddCommonParamFields(doc, dict);
            dict.Add("ctl00$Main$SkeletonCtrl_160$drpSearchParam", "Map Number");
            dict.Add("ctl00$Main$SkeletonCtrl_160$txtSearchParam", parcelNumber);
            dict.Add("ctl00$Main$SkeletonCtrl_160$gvAssessor$ctl02$btnSelectRecord", "View");

            return WebQuery.GetStringFromParameters(dict);
        }

        private static string GetValueOfCell(HtmlDocument doc, int index) {
            var table = doc.DocumentNode.SelectSingleNode("//table[@id='ctl00_Main_SkeletonCtrl_160_gvAssessor']");
            if (table == null) {
                LogMessageCodeAndThrowException("Table not found", doc.DocumentNode.OuterHtml);
            }
            var row = table.SelectSingleNode(string.Format(".//tr[@class='gvRow'][1]/td[{0}]", index+1));
            if (row == null) {
                LogMessageCodeAndThrowException(string.Format("Cell index {0} not found", index+1), doc.DocumentNode.OuterHtml);
            }
            return WebQuery.Clean(row.InnerText);
        }

        private string GetSearchParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            AddCommonParamFields(doc, dict);
            dict.Add("ctl00$Main$SkeletonCtrl_160$drpSearchParam", "Map Number");
            dict.Add("ctl00$Main$SkeletonCtrl_160$txtSearchParam", parcelNumber);
            dict.Add("ctl00$Main$SkeletonCtrl_160$btnSearch", "Search Records");
            return WebQuery.GetStringFromParameters(dict);
        }

        private HtmlDocument AcceptAgreementIfNecessary(HtmlDocument doc) {
            if (!doc.DocumentNode.OuterHtml.Contains("Do you accept the above statements")) {
                return doc;
            }
            string parameters = "";
            try
            {
                parameters = GetAgreementParameters(doc);
            }
            catch (Exception ex) {
                LogMessageCodeAndThrowException("Error getting agreement parameters", doc.DocumentNode.OuterHtml, ex);
            }
            doc = _webQuery.GetPost(SearchUrl, parameters, 1);
            return doc;
        }

        private string GetAgreementParameters(HtmlDocument doc) {
            var dict = new Dictionary<string, string>();
            AddCommonParamFields(doc, dict);
            dict.Add("__PREVIOUSPAGE", GetValueOfInput(doc, "__PREVIOUSPAGE"));
            dict.Add("ctl00$Main$SkeletonCtrl_160$btnAccept", "Yes, I do accept");

            return WebQuery.GetStringFromParameters(dict);
        }

        private void AddCommonParamFields(HtmlDocument doc, Dictionary<string, string> dict) {
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
        }

        #endregion
    }
}