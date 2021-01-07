using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using CTALookup.Scrapers;

namespace CTALookup.Maryland
{
    public class GenericScraper : MarylandScraper
    {
        public GenericScraper(string countyId) : base(countyId)
        {

        }

        public override Scraper GetClone()
        {
            return new GenericScraper(CountyId);
        }

        public override bool CanScrape(string county)
        {
            var c = county.ToLower();
            switch (c)
            {
                case "worcester":
                case "wicomico":
                case "washington":
                case "tailbot":
                case "st. mary's":
                case "queen anne's":
                case "prince george's":
                case "montgomery":
                case "kent":
                case "howard":
                case "harford":
                case "garret":
                case "frederick":
                case "dorchester":
                case "charles":
                    return true;
                default:
                    return false;
            }
        }

        private string GetCountyParameters(HtmlDocument doc, string countyId)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ToolkitScriptManager1", "ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$updatePanel2|ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlSearchType");
            dict.Add("__EVENTTARGET", "ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlSearchType");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__LASTFOCUS", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("q", "Search");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$hideBanner", "false");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlCounty", countyId);
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlSearchType", "02");
            dict.Add("__ASYNCPOST", "true");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetValueOfAjaxInput(HtmlDocument doc, string field)
        {
            var m = Regex.Match(doc.DocumentNode.OuterHtml, Regex.Escape(field) + @"\|" + @"(.+?)\|");
            return m.Groups[1].Value;
        }

        private string GetSearchParameters(HtmlDocument doc, string countyId, string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ToolkitScriptManager1", "ctl00$cphMainContentArea$ucSearchType$updatePanel1|ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$StepNavigationTemplateContainerID$btnStepNextButton");
            dict.Add("q", "Search");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$hideBanner", "false");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__LASTFOCUS", "");
            dict.Add("__VIEWSTATE", GetValueOfAjaxInput(doc, "__VIEWSTATE"));
            dict.Add("__EVENTVALIDATION", GetValueOfAjaxInput(doc, "__EVENTVALIDATION"));
            dict.Add("__ASYNCPOST", "true");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$StepNavigationTemplateContainerID$btnStepNextButton", "Next");

            //If Baltimore City
            if (countyId == "03")
            {
                parcelNumber = Regex.Replace(parcelNumber, @"(\d{2})\s*(\d{2})\s*(\d{4})\s*(\d{3})",
                                       m =>
                                       string.Format("{0} {1} {2} {3}", m.Groups[1].Value, m.Groups[2].Value,
                                                     m.Groups[3].Value, m.Groups[4].Value));
                string[] splitted = parcelNumber.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 4)
                {
                    Logger.LogMessageCodeAndThrowException(string.Format("Error parsing number: {0}. It must be divided in 4 parts", parcelNumber));
                }

                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtWard", splitted[0]);
                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtSection", splitted[1]);
                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtBlock", splitted[2]);
                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtLot", splitted[3]);
            }
            //If Anne Arundel
            else if (countyId == "02")
            {
                parcelNumber = Regex.Replace(parcelNumber, @"(\d{2})\s*(\d{3})\s*(\d+)",
                    m =>
                        string.Format("{0} {1} {2}", m.Groups[1].Value, m.Groups[2].Value,
                            m.Groups[3].Value));
                string[] splitted = parcelNumber.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 3)
                {
                    Logger.LogMessageCodeAndThrowException(
                        string.Format("Error parsing number: {0}. It must be divided in 3 parts", parcelNumber));
                }

                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtDistrict",
                    splitted[0]);
                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtSubDiv",
                    splitted[1]);
                dict.Add(
                    "ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtAccountIdentifier",
                    splitted[2]);
            }
            //If any other county...
            else
            {
                parcelNumber = Regex.Replace(parcelNumber, @"(\d{2})\s*(\d+)",
                    m =>
                        string.Format("{0} {1}", m.Groups[1].Value, m.Groups[2].Value));

                string[] splitted = parcelNumber.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 2)
                {
                    Logger.LogMessageCodeAndThrowException(
                        string.Format("Error parsing number: {0}. It must be divided in 2 parts", parcelNumber));
                }

                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtDistrict", splitted[0]);
                dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucEnterData$txtAccountIdentifier", splitted[1]);
            }

            return WebQuery.GetStringFromParameters(dict);
        }

        public override Item Scrape(string number)
        {
            _webQuery.ClearCookies();
            string baseUrl = "http://sdat.resiusa.org/RealProperty/Pages/default.aspx";
            var doc = _webQuery.GetSource(baseUrl, 1);

            string parameters = GetCountyParameters(doc, CountyId);
            doc = _webQuery.GetPost(baseUrl, parameters, 1, baseUrl, true);

            parameters = GetContinueParameters(doc, CountyId);

            doc = _webQuery.GetPost(baseUrl, parameters, 1, baseUrl, true);

            parameters = GetSearchParameters(doc, CountyId, number);

            InvokeSearching();

            doc = _webQuery.GetPost(baseUrl, parameters, 1, baseUrl, true);

            if (doc.DocumentNode.OuterHtml.Contains("There are no records that match your criteria"))
            {
                InvokeNoItemsFound();
                return null;
            }

            //Parse item
            var item = GetItem(doc);

            //Save image
            try
            {
                //                image = GetImage(doc, baseUrl);
            }
            catch (Exception e)
            {
                InvokeNotifyEvent("Image not found");
            }

            item.MapNumber = number;

            return item;
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            throw new NotImplementedException();
        }

        private string GetContinueParameters(HtmlDocument doc, string countyId)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ToolkitScriptManager1", "ctl00$cphMainContentArea$ucSearchType$updatePanel1|ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$StartNavigationTemplateContainerID$btnContinue");
            dict.Add("q", "Search");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$hideBanner", "false");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlCounty", countyId);
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$ucSearchType$ddlSearchType", "02");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__LASTFOCUS", "");
            dict.Add("__VIEWSTATE", GetValueOfAjaxInput(doc, "__VIEWSTATE"));
            dict.Add("__EVENTVALIDATION", GetValueOfAjaxInput(doc, "__EVENTVALIDATION"));
            dict.Add("__ASYNCPOST", "true");
            dict.Add("ctl00$cphMainContentArea$ucSearchType$wzrdRealPropertySearch$StartNavigationTemplateContainerID$btnContinue", "Continue");
            return WebQuery.GetStringFromParameters(dict);
        }

        private Image GetImage(HtmlDocument doc, string referrer)
        {
            string link;
            try
            {
                link =
                    WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'View Map')]").Attributes["href"].Value);
            }
            catch (Exception ex)
            {
                Logger.Log("No image found in current document");
                Logger.LogCode(doc.DocumentNode.OuterHtml);
                return null;
            }
            link = WebQuery.BuildUrl(link, "http://sdatcert3.resiusa.org/rp_rewrite/");

            InvokeNotifyEvent(string.Format("Opening url {0}", link));
            try
            {
                doc = _webQuery.GetSource(link, 1, referrer);
            }
            catch (Exception ex)
            {
                return null;
            }

            if (doc.DocumentNode.OuterHtml.Contains("A map was not found"))
            {
                Logger.Log("No image found in current document");
                Logger.LogCode(doc.DocumentNode.OuterHtml);
                return null;
            }

            InvokeNotifyEvent("Getting map picture");
            var imgUrl = WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//img[contains(@src, 'propertymap')]").Attributes["src"].Value);
            imgUrl = Regex.Replace(imgUrl, @"^\.\.", "http://sdatcert3.resiusa.org/rp_rewrite");
            return _webQuery.GetImage(imgUrl, 1);
        }

        public Item GetItem(HtmlDocument doc)
        {
            var item = new Item();

            item.Acreage =
                WebQuery.Clean(GetInnerText(doc, "span", "id", "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_Label19_0"));

            //Legal Description
            var node =
                doc.DocumentNode.SelectSingleNode(
                    "//span[@id='cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblLegalDescription_0']");
            item.LegalDescription = string.Join(", ",
                node.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText)).ToArray());

            //Physical Address
            node =
                doc.DocumentNode.SelectSingleNode(
                    "//span[@id='cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblPremisesAddress_0']");
            var address = node.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText)).ToList();

            item.PhysicalAddress1 = address[0];
            if (address.Count == 3 && address[2] != "")
            {
                item.PhysicalAddress1 += ", " + address[2];
            }

            try
            {
                AssignCityStateZipToPhysicalAddress(item, address[1], @"(.*)(\s+)(.*)");
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }

            //Owner Name
            var ownerValues = GetOwnerValues(doc);
            AssignNames(item, ownerValues[0]);
            if (ownerValues.Length > 1)
            {
                item.OwnerName += ", " + ownerValues[1];
                item.OwnerName = item.OwnerName.Trim();
            }

            //Owner Address
            node =
                doc.DocumentNode.SelectSingleNode(
                    "//span[@id='cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblMailingAddress_0']");
            var addr = node.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText)).ToArray();
            item.OwnerAddress = addr[0].TrimEnd(new[] { ' ', ',' });
            AssignCityStateZipToOwnerAddress(item, addr.Length > 2 ? addr[2] : addr[1]);

            if (addr.Length > 2 && !string.IsNullOrEmpty(addr[1]))
            {
                item.OwnerAddress += ", " + addr[1];
            }

            //item.Acreage = 
            item.LandValue = GetInnerText(doc, "span", "id",
                "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblBaseLandNow_0");
            item.ImprovementValue =
                GetInnerText(doc, "span", "id",
                    "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblBaseImproveNow_0");
            item.OwnerResident = GetInnerText(doc, "span", "id",
                "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblPrinResidence_0");
            item.HomesteadExcemption = GetInnerText(doc, "span", "id",
                "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblHomeStatus_0");

            AssignTransferValues(doc, item);


            item.Description = GetDescription(doc);
            item.Images = GetImages(doc);

            return item;
        }

        private string[] GetOwnerValues(HtmlDocument doc)
        {
            string owner1 = GetInnerText(doc, "span", "id",
                "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblOwnerName_0");
            string owner2 = null;
            try
            {
                owner2 = GetInnerText(doc, "span", "id",
                    "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblOwnerName2_0");
            }
            catch
            {

            }

            return string.IsNullOrEmpty(owner2) ? new string[] { owner1 } : new string[] { owner1, owner2 };
        }

        private void AssignTransferValues(HtmlDocument doc, Item item)
        {
            var node =
                doc.DocumentNode.SelectSingleNode("//span[contains(text(), 'ARMS LENGTH IMPRO')]");
            if (node == null)
            {
                return;
            }

            var tdNodes = node.ParentNode.ParentNode.PreviousSibling.PreviousSibling.SelectNodes("./td");
            item.TransferDate = WebQuery.Clean(tdNodes[1].InnerText).Replace("Date:", "").Trim();
            item.TransferPrice = WebQuery.Clean(tdNodes[2].InnerText).Replace("Price:", "").Trim();
        }

        private string GetDescription(HtmlDocument doc)
        {
            var description = GetInnerText(doc, "span", "id",
                "cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblUse_0");
            var node =
                doc.DocumentNode.SelectSingleNode(
                    "//span[@id='cphMainContentArea_ucSearchType_wzrdRealPropertySearch_ucDetailsSearch_dlstDetaisSearch_lblDedRef_0']");
            var values = node.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText)).ToArray();

            foreach (var v in values)
            {
                string clean = Regex.Replace(v, @"\d+\)", "").Trim();
                if (!string.IsNullOrEmpty(clean))
                {
                    description += ", " + clean;
                }
            }

            return description;
        }

        private string GetValueOfVerticalField(HtmlDocument doc, string field)
        {
            var values = GetValuesOfVerticalField(doc, field);
            string result = "";
            for (int i = 0; i < values.Count; i++)
            {
                if (string.IsNullOrEmpty(values[i]))
                {
                    continue;
                }
                result += values[i];
                if (i != values.Count - 1)
                {
                    result += ", ";
                }
            }

            if (result.EndsWith(", "))
            {
                result = result.Substring(0, result.Length - 2);
            }

            return result;
        }

        private IList<string> GetValuesOfVerticalField(HtmlDocument doc, string field)
        {
            var fieldNode =
                doc.DocumentNode.SelectSingleNode(string.Format("//td[@class='detailbold']/a[contains(text(), '{0}')]",
                                                                field));
            var trNode = fieldNode.AncestorsAndSelf("tr").First();
            var index = trNode.SelectNodes("./td").IndexOf(fieldNode.ParentNode);

            var trNodes = trNode.ParentNode.SelectNodes("./tr").Skip(1).ToList();
            IList<string> values = new List<string>();
            foreach (var node in trNodes)
            {
                var tdNodes = node.SelectNodes("./td");
                var tdNode = tdNodes[index];
                values.Add(WebQuery.Clean(tdNode.InnerText));
            }

            return values;
        }

        private string GetValueOfHorizontalField(HtmlDocument doc, string field)
        {
            var fieldNode =
                doc.DocumentNode.SelectSingleNode(string.Format("//td[@class='detailbold']/a[contains(text(), '{0}')]",
                                                                field));
            return WebQuery.Clean(fieldNode.ParentNode.NextSibling.InnerText);
        }

        private string[] GetValuesOfHorizontalField(HtmlDocument doc, string field)
        {
            var fieldNode =
                doc.DocumentNode.SelectSingleNode(string.Format("//td[@class='detailbold']/a[contains(text(), '{0}')]",
                                                                field));

            return fieldNode.ParentNode.NextSibling.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText)).ToArray();
        }

        private static string GetSearchUrl(string county, string number)
        {
            //If Baltimore City
            if (county == "03")
            {
                number = Regex.Replace(number, @"(\d{2})\s*(\d{2})\s*(\d{4})\s*(\d{3})",
                                       m =>
                                       string.Format("{0} {1} {2} {3}", m.Groups[1].Value, m.Groups[2].Value,
                                                     m.Groups[3].Value, m.Groups[4].Value));
                string[] splitted = number.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 4)
                {
                    Logger.LogMessageCodeAndThrowException(string.Format("Error parsing number: {0}. It must be divided in 4 parts", number));
                }
                return
                    string.Format(
                        "http://sdatcert3.resiusa.org/rp_rewrite/details.aspx?County={0}&SearchType=ACCT&Ward={1}&Section={2}&Block={3}&Lot={4}",
                        county, splitted[0], splitted[1], splitted[2], splitted[3]
                        );
            }
            //If Anne Arundel
            if (county == "02")
            {
                number = Regex.Replace(number, @"(\d{2})\s*(\d{3})\s*(\d+)",
                                       m =>
                                       string.Format("{0} {1} {2}", m.Groups[1].Value, m.Groups[2].Value,
                                                     m.Groups[3].Value));
                string[] splitted = number.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length != 3)
                {
                    Logger.LogMessageCodeAndThrowException(string.Format("Error parsing number: {0}. It must be divided in 3 parts", number));
                }
                return
                    string.Format(
                        "http://sdatcert3.resiusa.org/rp_rewrite/details.aspx?County={0}&SearchType=ACCT&District={1}&AccountNumber={2}&subDiv={3}",
                        county,
                        splitted[0],
                        splitted[2],
                        splitted[1]);
            }

            //If any other county...
            number = number.Replace(" ", "").Trim();
            return
                string.Format(
                    "http://sdatcert3.resiusa.org/rp_rewrite/details.aspx?County={0}&SearchType=ACCT&District={1}&AccountNumber={2}",
                    county, number.Substring(0, 2), number.Substring(2, number.Length - 2));
        }
    }
}
