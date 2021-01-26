using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CTALookup.Scrapers;
using HtmlAgilityPack;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CTALookup.Arizona {
    public class ScraperPima : ScraperArizona {
        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:pima";
        }

        public override Scraper GetClone()
        {
            return new ScraperPima();
        }

        public override Item Scrape(string parcelNumber)
        {
            parcelNumber = parcelNumber.Replace(" ", "");
            InvokeOpeningSearchPage();

            //https://gis.pima.gov/maps/detail.cfm?parcel=304291600
            //var doc = _webQuery.GetSource("http://www.to.pima.gov/pcto/tweb/property_inquiry/show/" + parcelNumber.Trim().Replace("-", ""), 1);

            var doc = _webQuery.GetSource("https://gis.pima.gov/maps/detail.cfm?parcel=" + parcelNumber.Trim().Replace("-", ""), 1);

            //_webQuery.GetSource("http://www.to.pima.gov/property-information/property-inquiry", 1);
            //var parameters = GetInquiryParams(doc, parcelNumber);
            InvokeSearching();
            //doc = _webQuery.GetPost("http://www.to.pima.gov/pcto/tweb/property_inquiry", parameters, 1);

            var item = GetInquiryItem(doc);

            string xp = @"//*[@id='content']/center[1]/table/tbody/tr[2]/td[3]/table/tbody/tr/td/table[1]/tbody";
            //            var pyd = doc.DocumentNode.SelectSingleNode("//div[@id='content']//table[@class='data']//td/span[@class='boldwarning']");

            xp = @"//div[@id='content']//table[@class='data color-light']//table";
            var pyd = doc.DocumentNode.SelectSingleNode(xp);

            string pydExpresiion = "PRIOR YEAR DELINQUENCY";
            //?            if (pyd != null && WebQuery.Clean(pyd.InnerText).Equals(pydExpresiion))
            if (pyd != null && WebQuery.Clean(pyd.InnerText).Contains(pydExpresiion))
            {
                item.Description = pydExpresiion;
                item.SetReasonToOmit(pydExpresiion);
            }
            //?
            ///Home/FindParcel?ts=1515527922904&parcelNumber=303083720
            ///            string oldContentType = _webQuery.ContentType;
            ChromeOptions option = new ChromeOptions();
            option.AddArgument("headless");

            option.AddArgument("--headless");

            String PROXY = "http://lum-customer-hl_389267c6-zone-static:e7ecycprce6a@zproxy.lum-superproxy.io:22225";
            //options.AddArguments("user-data-dir=path/in/your/system");

            Proxy proxy = new Proxy();

            proxy.HttpProxy = PROXY;
            proxy.SslProxy = PROXY;
            proxy.FtpProxy = PROXY;

            option.Proxy = proxy;

            option.AcceptInsecureCertificates = true;
            option.PageLoadStrategy = PageLoadStrategy.Normal;

            //var chromeDriverService = new ChromeDriver(option);
            IWebDriver driver = new ChromeDriver(option);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(120);

            
            //Navigate to  page
            //"http://www.asr.pima.gov/Home/ParcelSearch" old url
            string newUrl = "https://www.asr.pima.gov/?aspxerrorpath=/Home/ParcelSearch&aspxerrorpath=/Home/ParcelSearch";
            driver.Navigate().GoToUrl(newUrl);
            
            //
            IWebElement parcelInput = driver.FindElement(By.Id("parcelInput"));
            //Perform Ops
            parcelInput.SendKeys(parcelNumber);
            parcelInput.SendKeys(OpenQA.Selenium.Keys.Enter);

            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                var e = wait.Until(x => x.FindElement(By.XPath("//*[@id='ValuData']/table/tbody")));
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element with locator:  was not found in current context page.");

            }
            string page = driver.PageSource;

            doc.LoadHtml(page);

            GetItem(doc, item);

            //?
            /*
        doc = _webQuery.GetSource(
            "http://www.asr.pima.gov/links/frm_AdvancedSearch_v2.aspx?search=Parcel://www.asr.pima.gov/links/frm_AdvancedSearch_v2.aspx",
            1);

        var parameters = GetParameters(doc, parcelNumber);

        InvokeSearching();
        try
        {
            doc =
                _webQuery.GetPost(
                    "http://www.asr.pima.gov/links/frm_AdvancedSearch_v2.aspx?search=Parcel%3a%2f%2fwww.asr.pima.gov%2flinks%2ffrm_AdvancedSearch_v2.aspx",
                    parameters, 1);
            GetItem(doc, item);
        }
        catch { }
        */
            //item = GetItem(doc);


            //Get Mailing Address

            /*doc = _webQuery.GetSource(string.Format("http://gis.pima.gov/maps/detail.cfm?parcel={0}", parcelNumber), 1);

            var node = doc.DocumentNode.SelectSingleNode("//table[@cellspacing='0' and @border='0']//tr/td[1]");

            if (node == null) {
                return item;
            }

            var texts =
                node.SelectNodes("./text()")
                    .Select(x => WebQuery.Clean(x.InnerText).Trim(','))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

            //The address is the last two lines
            item.MailingAddress = texts[texts.Count - 2];

            string cityStateZip = texts[texts.Count - 1];

            AssignCityStateZipToMailingAddress(item, cityStateZip, @"([^\s]+)\s+([^\s]+)\s+([^\s]+)");
            */
            GetAdditionalOwmer(item);
            driver.Quit();
            //chromeDriverService.Dispose();

            return item;
        }

        private Item GetInquiryItem(HtmlDocument doc)
        {
            var item = new Item();
            var node = GetTrByName(doc, "NAME/ADDRESS");
            if (node != null)
            {
                var strs = node.SelectNodes("td/* | td/text()").Select(x => WebQuery.Clean(x.InnerText)).ToList();
                strs.RemoveAll(s => String.IsNullOrEmpty(s));
                if (strs.Count > 1 && strs[1].ToLower().StartsWith("c/o")) strs.RemoveAt(1);
                if (strs.Count > 3 && !Char.IsNumber(strs[1].First()))
                {
                    AssignNames(item, strs[0], strs[1]);
                    strs.RemoveAt(0);
                }
                else AssignNames(item, strs[0]);
                
                if (strs.Count > 2)
                {
                    item.MailingAddress = strs[strs.Count-2];
                    AssignCityStateZipToMailingAddress(item, strs.Last());
                }
                else if (strs.Count > 1) item.MailingAddress = strs[1];


                node = node.ParentNode.Sibling("table");//GetTrByName(doc, "PROPERTY ADDRESS");
                if (node != null)
                {
                    node = node.SelectNodes("tr")[1];
                    item.PhysicalAddress1 = WebQuery.Clean(node.InnerText);
                }
            }
            node = GetTrByName(doc, "DESCRIPTION");
            if (node != null) item.LegalDescription = WebQuery.Clean(node.InnerText);
            node = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'boldwarning')]");
            if (node != null) item.Description = WebQuery.Clean(node.InnerText) + "    ";
            return item;
        }

        private HtmlNode GetTrByName(HtmlDocument doc, String Header)
        {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//tr[contains(th/text(),'{0}')]", Header));
            if (node != null) node = node.Sibling("tr");
            return node;
        }

        private String GetInquiryParams(HtmlDocument doc, string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("date", GetValueOfInput(doc, "date"));
            dict.Add("statecode", parcelNumber);
            dict.Add("submit", "SEARCH");
            dict.Add("taxyear", (DateTime.Now.Year).ToString());//GetValueOfComboBox(doc, "taxyear"));
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetParameters(HtmlDocument doc, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            dict.Add("__LASTFOCUS", "");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("ctl00$ContentPlaceHolder1$txtPID", parcelNumber);
            dict.Add("ctl00$ContentPlaceHolder1$btnParcel", "Search Parcel");
            dict.Add("ctl00$ContentPlaceHolder1$txtTaxpay", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtSingleStreetAddress", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtStreetRangeBegin", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtStreetRangeEnd", "");
            dict.Add("ctl00$ContentPlaceHolder1$ddlStDir", "--select--");
            dict.Add("ctl00$ContentPlaceHolder1$txtStreetName", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtMap", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtPlat", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtBlock", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtLot", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtSequence", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtDocket", "");
            dict.Add("ctl00$ContentPlaceHolder1$txtPage", "");
            dict.Add("ctl00$ContentPlaceHolder1$ddlTown", "-1");
            dict.Add("ctl00$ContentPlaceHolder1$ddlRange", "-1");
            dict.Add("ctl00$ContentPlaceHolder1$ddlSection", "-1");
            dict.Add("ctl00$ContentPlaceHolder1$endTY", GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$endTY"));
            dict.Add("ctl00$ContentPlaceHolder1$currTY", GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$currTY"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldSearchType", "");
            dict.Add("ctl00$ContentPlaceHolder1$fieldCriteria1",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCriteria1"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldCriteria2",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCriteria2"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldCriteria3",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCriteria3"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldCriteria4",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCriteria4"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldCriteria5",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCriteria5"));
            dict.Add("ctl00$ContentPlaceHolder1$fieldCount",
                GetValueOfInput(doc, "ctl00$ContentPlaceHolder1$fieldCount"));
            return WebQuery.GetStringFromParameters(dict);
        }

        private void AssignPhysicalAddress(Item item, HtmlDocument doc) {
            ////*[@id="TaxpyrLegal"]/table/tbody/tr/td[1]
            var tdNodes = doc.DocumentNode.SelectNodes("//table[@id='ContentPlaceHolder1_gvSitus']//tr[2]/td");
            if (tdNodes == null) {
                return;
            }
            item.PhysicalAddress1 = WebQuery.Clean(tdNodes[0].InnerText) + " " + WebQuery.Clean(tdNodes[2].InnerText);
            item.PhysicalAddressCity = WebQuery.Clean(tdNodes[3].InnerText).Replace("County", "").Trim();
            item.PhysicalAddressState = "AZ";

        }

        private void GetItem(HtmlDocument doc, Item item) {
            if (doc.DocumentNode.OuterHtml.Contains("This parcel is no longer active")) {
                GetItemFromHistory(doc, item);
                return;
            }
            item.Images = GetImages(doc);
            GetItemAux(doc, item);
        }

        private void GetItemAux(HtmlDocument doc, Item item) {
            //var item = new Item();
            // try 
            {
  //              item.MapNumber =
   //                 WebQuery.Clean(
   //                     doc.GetElementbyId("ContentPlaceHolder1_txtParcelSearch").Attributes["value"].Value)
  //                      .Replace("-", "")
  //                      .Replace(" ", "");

                //string name = WebQuery.Clean(doc.GetElementbyId("ContentPlaceHolder1_lbMail1").InnerText);

                //AssignNames(item, name);

                AssignPhysicalAddress(item, doc);
                AssignOwnerAddress(item, doc);

                //item.LegalDescription = WebQuery.Clean(doc.GetElementbyId("ContentPlaceHolder1_lbLegal1").InnerText);

//?                item.MarketValue = GetTotalFCV(doc);
                var nodes = doc.DocumentNode.SelectNodes("//*[@id='ValuData']/table/tbody/tr");
                int maxRows = nodes.Count();
                nodes = doc.DocumentNode.SelectNodes(String.Format("//*[@id='ValuData']/table/tbody/tr[{0}]/td[2]", maxRows));
                item.PropertyType = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes(String.Format("//*[@id='ValuData']/table/tbody/tr[{0}]/td[4]", maxRows));
                item.LandValue = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes(String.Format("//*[@id='ValuData']/table/tbody/tr[{0}]/td[5]", maxRows));
                item.ImprovementValue = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));

                nodes = doc.DocumentNode.SelectNodes(String.Format("//*[@id='ValuData']/table/tbody/tr[{0}]/td[6]", maxRows));
                item.MarketValue = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));

                ////*[@id="ResdChr"]/table[1]/tbody
                //?
          //      nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[1]/td[4]");
           //     item.PropertyType = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[4]/td[2]");
                string sqft = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[5]/td[2]");
                string constructionYear = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[8]/td[4]");
                string pool = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[8]/td[2]");
                string quality = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                nodes = doc.DocumentNode.SelectNodes("//*[@id='ResdChr']/table[1]/tbody/tr[12]/td[4]");
                string totalFCV = (nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText));
                ///html/body/div[2]/div[1]/div[1]/div/div[1]/div[1]/div[1]/table/tbody/tr[2]/td[2]/div/select/option[1]
                nodes = doc.DocumentNode.SelectNodes("/html/body/div[2]/div[1]/div[1]/div/div[1]/div[1]/div[1]/table/tbody/tr[2]/td[2]/div/select/option");
                string yearDisp = "";
                int i;
                for (i = 0; i < nodes.Count(); i++)
                    if(nodes[i].OuterHtml.Contains("selected"))
                    {
                        string[] t = nodes[i].OuterHtml.Split(' ');
                        for(int j=0; j< t.Length; j++)
                        {
                            if(t[j].StartsWith("value="))
                            {
                                yearDisp = t[j].Split('=')[1].Replace("\\", "").Replace("\"", "");
                                break;
                            }
                        }
                    }
                //? 
                item.Notes = string.Format("YR: {0},   SQFT: {1},   Pool: {2},  QUALITY: {3}", constructionYear, sqft, pool, quality);

                /* ?
                 item.PropertyType = GetTableValueLastRowColumn(doc, 1);
                 item.Notes = string.Format("{0}  YR: {1},   SQFT: {2},   Pool: {3},  QUALITY: {4}",
                                    GetSpanValue(doc, "ContentPlaceHolder1_lbPropertyType"),
                                    GetSpanValue(doc, "ContentPlaceHolder1_lbYear_disp"),
                                    GetSpanValue(doc, "ContentPlaceHolder1_lbSQFT"),
                                    GetSpanValue(doc, "ContentPlaceHolder1_lbPoolArea"),                    
                                    GetSpanValue(doc, "ContentPlaceHolder1_lbQLT"));
                item.Description += GetValue(doc, "TOTAL FCV");
                */
                //AssignAdditionalNote(doc, item, "Total Livable Area", "SQFT");
                //AssignAdditionalNote(doc, item, "Effective Construction Year", "YR");
                //AssignAdditionalNote(doc, item, "Pool", null, x => x.Trim() != "0");
            }
            //           catch (Exception ex) {
            //               LogThrowErrorInField(doc, ex);
            //           }

            //return item;
        }

        private void GetItemFromHistory(HtmlDocument doc, Item item) {
            //var item = new Item();
            try {
                string name = GetNameFromHistory(doc);
                if (String.IsNullOrEmpty(item.OwnerFirstName) && String.IsNullOrEmpty(item.Company))
                    AssignNames(item, name);

                AssignOwnerAddressFromHistory(item, doc);

                //item.LegalDescription = GetLegalDescriptionFromHistory(doc);

//                item.MarketValue = GetMarketValue(doc);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }

            //return item;
        }

        private HtmlNode GetTaxTable(HtmlDocument doc) {
            var tdNode = doc.DocumentNode.SelectSingleNode("//td[contains(./text(), 'Taxpayer Info')]");
            return tdNode.Ancestors("table").First();
        }

        private string GetLegalDescriptionFromHistory(HtmlDocument doc) {
            var n = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'Legal Description')]");
            n = n.Sibling("td");

            return WebQuery.Clean(n.InnerText);
        }

        private void AssignOwnerAddressFromHistory(Item item, HtmlDocument doc) {
            var table = GetTaxTable(doc);

            var trs = table.SelectNodes("./tr").Select(x => WebQuery.Clean(x.InnerText)).ToList();
            var text = "";
            if (string.IsNullOrEmpty(trs[5]))
            {
                if (string.IsNullOrEmpty(trs[4]))
                {
                    item.OwnerAddress = trs[2];
                    text = trs[3];
                }
                else
                {
                    item.OwnerAddress = trs[3];
                    text = trs[4];
                }
            }
            else
            {
                item.OwnerAddress = trs[4];
                text = trs[5];
            }

            var m = Regex.Match(text, @"(.+)\s+(.+)");
            item.OwnerCity = m.Groups[1].Value;
            item.OwnerState = m.Groups[2].Value;
            item.OwnerZip = trs[6];
        }

        private string GetNameFromHistory(HtmlDocument doc) {
            var table = GetTaxTable(doc);

            var trs = table.SelectNodes("./tr").Select(x => WebQuery.Clean(x.InnerText)).ToList();

            string name = trs[1];

            var name2 = trs[2];

            if (!string.IsNullOrEmpty(name2)) {
                if (name2.ToLower().StartsWith("c/o")) {
                    name += " " + name2;
                }
            }
            return name;
        }

        private void AssignAdditionalNote(HtmlDocument doc, Item item, String TableCaption, String Abbr = null, Func<String,Boolean> FuncYesNo = null )
        {
            var res = GetValue(doc, TableCaption);
            if (String.IsNullOrEmpty(res) || res == "n/a") return;
            if (FuncYesNo != null) res = FuncYesNo(res) ? "Y" : "N";
            item.Notes += String.Format("    {0}: {1}", String.IsNullOrEmpty(Abbr) ? TableCaption : Abbr, res);
        }

        private string GetSpanValue(HtmlDocument doc, string name)
        {
            var n = doc.DocumentNode.SelectSingleNode(string.Format("//span[@id='{0}']", name));
            if (n == null)
            {
                return "n/a";
            }

            return WebQuery.Clean(n.InnerText);
        }

        private string GetValue(HtmlDocument doc, String Caption)
        {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//td[contains(text(), '{0}')]", Caption));
            if (node == null) return "";
            var tdNode = node.Sibling("td");

            return WebQuery.Clean(tdNode.InnerText);
        }

        private string GetTableValueLastRowColumn(HtmlDocument doc, int columnID)
        {
            ////*[@id="ValuData"]/table/tbody/tr[1]/td[6]
            //?           var nodes = doc.DocumentNode.SelectNodes("//table[@id='tableValuation']/tr[4]//td");
            var nodes = doc.DocumentNode.SelectNodes(String.Format("//*[@id='ValuData']/table/tbody/tr[1]/td[{0}]", columnID));
 //?           return nodes == null ? "" : WebQuery.Clean(nodes[columnID].InnerText);
            return nodes == null ? "" : WebQuery.Clean(nodes[0].InnerText);
        }

        private string GetMarketValue(HtmlDocument doc) {
            return GetTableValueLastRowColumn(doc, 4);            
        }

        private string GetTotalFCV(HtmlDocument doc)
        {
 //?           return GetTableValueLastRowColumn(doc, 3);
            return GetTableValueLastRowColumn(doc, 6);
        }

        private void AssignOwnerAddress(Item item, HtmlDocument doc)
        {
            try
            {
                var tdNodes = doc.DocumentNode.SelectNodes("//*[@id='TaxpyrLegal']/table/tbody/tr/td[1]");
                string[] address = WebQuery.Clean(tdNodes[0].InnerText).Replace('\r', ' ').Replace("<br>", "").Replace('\0',' ').Split('\n');

                for (int i = 0; i < address.Length; i++)
                    address[i] = address[i].Trim();
                item.OwnerFirstName = address[0];
                string local;
                if (address[3] == "")
                {
                    item.OwnerAddress = address[1];
                    local = address[2];
                }
                else
                {
                    item.OwnerFirstName = address[0] + " " + address[1];
                    item.OwnerAddress = address[2];
                    local = address[3];
                }
                item.OwnerCity = local;
                item.OwnerState = "";
                if (local.Length > 3)
                {
                    item.OwnerCity = local.Substring(0, local.Length - 2).Trim();
                    item.OwnerState = local.Substring(local.Length - 2);
                }
                item.OwnerZip = address[5].Trim();

                return;
                var table = doc.GetElementbyId("ContentPlaceHolder1_lbMail1").Ancestors("table").First();
                var trs = table.SelectNodes("./tr");
                // //          var tdNodes = doc.DocumentNode.SelectNodes("//*[@id='TaxpyrLegal']/table/tbody/tr/td[1]");

                IList<HtmlNode> trNodes = new List<HtmlNode>();
                foreach (var node in trs)
                {
                    if (string.IsNullOrEmpty(WebQuery.Clean(node.InnerText)) || node.InnerText.Contains("Taxpayer Information"))
                    {
                        continue;
                    }
                    trNodes.Add(node);
                }
                if (trNodes.Count > 3) item.OwnerAddress = WebQuery.Clean(trNodes[trNodes.Count - 3].InnerText);
                string cityAddress = (trNodes.Count > 2) ? WebQuery.Clean(trNodes[trNodes.Count - 2].InnerText) : "";

                var n = doc.GetElementbyId("ContentPlaceHolder1_lbZip");
                var x = n.Sibling("span");

                string zipCode = WebQuery.Clean(n.InnerText);

                if (x != null)
                {
                    zipCode += WebQuery.Clean(x.InnerText.Replace("- ", "-"));
                }

                var m = Regex.Match(cityAddress, @"(.+)\s+(.+)");
                if (!m.Success)
                {
                    item.OwnerAddress += " " + cityAddress + " " + zipCode;
                }
                else
                {
                    item.OwnerCity = m.Groups[1].Value;
                    item.OwnerState = m.Groups[2].Value;
                    item.OwnerZip = zipCode;
                }

            }
            catch (Exception ex) { }
        }


        private String GetSearchParams(String Param)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("assessorno", "");
            dict.Add("propaddress", "");
            dict.Add("query", Param);
            dict.Add("submit", "SEARCH");
            dict.Add("taxpayerno", "");
            dict.Add("zip", "");
            return WebQuery.GetStringFromParameters(dict);
      
        }
        private void GetAdditionalOwmer(Item item)
        {
            if (String.IsNullOrEmpty(item.MailingAddress)) return;
            var doc = _webQuery.GetSource("http://www.to.pima.gov/property-information/property-search", 1);
            var parameters = GetSearchParams(item.MailingAddress);
            doc = _webQuery.GetPost("http://www.to.pima.gov/pcto/tweb/property_search", parameters, 1, "http://www.to.pima.gov/pcto/tweb/property_search");
            var node = doc.DocumentNode.SelectSingleNode("//table[@class='data']/tr[2]/td[1]/text()");
            item.MailingAddressOwner = node == null ? "n/a" : WebQuery.Clean(node.InnerText);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }
    }
}