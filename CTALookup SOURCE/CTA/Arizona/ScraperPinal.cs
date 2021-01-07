using System;
using System.Collections.Generic;
using System.Linq;
using CTALookup.Scrapers;
using HtmlAgilityPack;
using System.Web;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CTALookup.Arizona
{
    public class ScraperPinal : ScraperArizona
    {
        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:pinal";
        }

        public override Scraper GetClone()
        {
            return new ScraperPinal();
        }

        public override Item Scrape(string parcelNumber) {
            var item = new Item();

            //parcelNumber = parcelNumber.Replace("-", "");
            //Get Mailing Address info
            try
            {
                var doc = _webQuery.GetSource("https://treasurer.pinalcountyaz.gov/ParcelInquiry/Parcel/NewParcel", 1);


                var dict = new Dictionary<string, string>
                {
                    {"parcelNumber_input", parcelNumber},
                    {"parcelNumber", parcelNumber},
                    {"ReturnController", ""},
                    {"ReturnAction", ""}
                };
                var parameters = WebQuery.GetStringFromParameters(dict);

                doc = _webQuery.GetPost("https://treasurer.pinalcountyaz.gov/ParcelInquiry/Main/ParcelEntry", parameters, 1);

                var addNotes = "";
                if (!doc.DocumentNode.OuterHtml.Contains("Invalid Parcel Number"))
                {
                    var Node = doc.DocumentNode.SelectSingleNode("//fieldset[@class='addressblock']");
                    if (Node != null)
                    {
                        var texts =
                            Node.SelectNodes("./text()")
                                .Select(x => WebQuery.Clean(x.InnerText).Trim(','))
                                .Where(x => !string.IsNullOrEmpty(x)).ToList();

                        //MODIFY: Names already set from assessor page
                        //if (texts.Count > 3 && !Char.IsNumber(texts[1].First()))
                        //    AssignNames(item, texts[0], texts[1]);
                        //else AssignNames(item, texts[0]);

                        if (texts.Count < 3)
                            item.MailingAddress = texts.Last();
                        else
                        {
                            item.MailingAddressOwner = texts[texts.Count - 3];
                            item.MailingAddress = texts[texts.Count - 2];
                            string cityStateZip = texts[texts.Count - 1];

                            AssignCityStateZipToMailingAddress(item, cityStateZip, @"(.+),\s*(.+)\s+(.+)");
                        }
                    }

                    Node = doc.DocumentNode.SelectSingleNode("//fieldset[@class='legalblock']");
                    if (Node != null)
                    {
                        var texts = Node.SelectNodes("./text()").Select(x => WebQuery.Clean(x.InnerText).Trim(',')).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        item.LegalDescription = WebQuery.Clean(String.Join(" ", texts));
                    }

                    var nodes = doc.DocumentNode.SelectNodes("//div[@id='Grid']//table//tr/td[3]/*/text()");
                    if (nodes != null && nodes.Count > 0)
                    {
                        if (nodes.All(x => WebQuery.Clean(x.InnerText) == "TAX")) addNotes = "NO PRIOR LIENS";
                    }
                    Node = doc.DocumentNode.SelectSingleNode("//div[@id='Grid']//table//tr/td[contains(text(), '2014')]/../td[7]");
                    if (Node != null && WebQuery.Clean(Node.InnerText) == "$0.00")
                    {
                        addNotes = "PAID, " + addNotes;

                    }
                }
                var grid = doc.DocumentNode.SelectNodes("//div[@id='Grid']/table/tbody/tr");
                foreach (var row in grid)
                {
                    var columns = row.ChildNodes;
                    string status = WebQuery.Clean(columns[2].InnerText);
                    if (status.Equals("PUR"))
                    {
                        item.Description = "PRIOR YEAR DELINQUENCY";
                        item.SetReasonToOmit(item.Description);
                        break;
                    }
                }
            }catch(Exception e)
            {
                Logger.LogException(e);
            }
            InvokeOpeningSearchPage();
            HtmlDocument assessorDoc = new HtmlDocument();
            /*
            var assessorDoc = _webQuery.GetSource("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", 1);

            var assessorParameters = GetSearchParameters(assessorDoc, parcelNumber, item);

            InvokeSearching();
            //http://www.pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx
 //?                       assessorDoc = _webQuery.GetPost("http://www.pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", assessorParameters, 1);
 */
//    assessorDoc = _webQuery.GetSource("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx?b=505&m=28&p=018&s=H", 1);

            //?

            ChromeOptions option = new ChromeOptions();
            option.AddArgument("--headless");

            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;
            IWebDriver driver = new ChromeDriver(chromeDriverService, option);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);

            string[] splitted = parcelNumber.Split('-');

            string book = splitted[0];
            string map = splitted[1];
            string text = splitted[2];

            string parcel = string.Join("", text.Take(text.Length - 1).Select(x => x.ToString()).ToArray());
            string split = text[text.Length - 1].ToString();

            //Navigate to  page
            driver.Navigate().GoToUrl(String.Format("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx?b={0}&m={1}&p={2}&s={3}", book, map, parcel, split));
            //
            //Perform Ops
            
             try
             {
                 var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                 var e = wait.Until(x => x.FindElement(By.Id("aspnetForm")));
             }
             catch (NoSuchElementException)
             {
                 Console.WriteLine("Element with locator:  was not found in current context page.");
             }

            string page = driver.PageSource;

            assessorDoc.LoadHtml(page);


//?            assessorDoc = ProcessSelectingFirst(assessorDoc);
            GetItem(assessorDoc, item);
            GetAdditionalOwmer(item);
            //if (String.IsNullOrEmpty(addNotes)) item.Notes = addNotes + "  " + item.Notes;

            driver.Quit();
            chromeDriverService.Dispose();

            return item;
        }

        private string GetValue(HtmlDocument doc, string name, string tag = "span") {
            var n = doc.DocumentNode.SelectSingleNode(string.Format("//{0}[@id='{1}']", tag, name));
            if (n == null) {
                return "n/a";
            }

            return WebQuery.Clean(n.InnerText);
        }

        private void GetItem(HtmlDocument doc, Item item) {
            try {
                item.MapNumber =
                    GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_ParcelNumber");
                //item.LegalDescription = GetValue(doc,"ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_LegalInformation");
                item.Acreage = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_ParcelSize");
                //ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_ParcelSize
                AssignPhysicalAddress(doc, item);


                AssignOwnerName(doc, item);

                AssignPropertyType(doc, item);

                item.OwnerAddress = GetValue(doc,
                    "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_Address");
                item.OwnerCity = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_City");
                item.OwnerZip = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_ZipCode");
                item.OwnerState = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_State");

                item.MarketValue = GetValue(doc,
                    "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_FullCashValue");
                item.OwnerResident = GetOwnerName(doc);
                item.Images = GetImages(doc);
                GetImprovement(doc, item, "1");
                GetImprovement(doc, item, "2");


                item.Notes = string.Format("{0} YR:{1}, Sq Ft:{2}, Pool:N/A, DoS:{3}, Price:{4}", 
                    GetPropertyType(doc),
                    GetConstYear(doc),                    
                    GetTotalSqrFeet(doc),                    
                    GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_DateOfSale"),
                    GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_SaleAmount")
                    );


                //var node = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Impr. Legal Class')]");
                //if (node != null)
                //{
                //    node = node.Sibling("td");
                //    item.PropertyType = WebQuery.Clean(node.InnerText);
                //}
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
        }

        private void GetImprovement(HtmlDocument doc, Item item, String Num)
        {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//td[contains(text(),'Imp:') and contains(following-sibling::td/text(),'{0}')]", Num));
            if (node != null)
            {
                node = node.Sibling("td").Sibling("td").Sibling("td");
                AddAdditionalNote(item, "Imp"+Num, WebQuery.Clean(node.InnerText));
            }
        }

        private void AssignPropertyType(HtmlDocument doc, Item item)
        {
            //string land = "Land LegalClass: ";
            //string impr = "Impr Legal Class: ";
            string landLegalClass =  GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_LandLegalClass");
            int hyphenIndex = -1;
            if (!string.IsNullOrEmpty(landLegalClass))
            {
                hyphenIndex = landLegalClass.IndexOf('-');
                if (hyphenIndex > 0)
                    landLegalClass = "LAND - " + landLegalClass.Substring(hyphenIndex + 1).Trim();
            }

            string imprLegalClass = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_ImprLegalClass");
            hyphenIndex = -1;
            if (!string.IsNullOrEmpty(imprLegalClass))
            {
                hyphenIndex = imprLegalClass.IndexOf('-');
                if (hyphenIndex > 0)
                    imprLegalClass = "IMPROVE - " + imprLegalClass.Substring(hyphenIndex + 1).Trim();
            }
            List<string> classes = new List<string> { imprLegalClass, landLegalClass };
            item.PropertyType = string.Join(", ", classes.Where(s=> !string.IsNullOrWhiteSpace(s)));           
        }


        private void AssignOwnerName(HtmlDocument doc, Item item) {
            string owner1 = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_Owner1");
            string owner2 = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_Owner2");
            string co = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_InCO");

            string ownerName = owner1;
            if (!string.IsNullOrEmpty(owner2)) {
                ownerName += ", " + owner2;
            }

            // Sometimes C/O appears in owner2.
            if (!string.IsNullOrEmpty(co))
            {
                ownerName += ", C/O " + co.Replace("c/o", "").Replace("C/O", "");
            }

            string text = ownerName;

            AssignNames(item, text);

            item.OwnerName = ownerName;

        }

        private void AssignPhysicalAddress(HtmlDocument doc, Item item) {
            var n =
                doc.DocumentNode.SelectSingleNode(
                    "//span[@id='ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_PropertyAddress']");
                        //*[@id="ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_PropertyAddress"]
            var nodes = n.SelectNodes("./text()");

            if (nodes == null) {
                return;
            }

            var texts = nodes.Select(x => WebQuery.Clean(x.InnerText)).Where(x => !string.IsNullOrEmpty(x)).ToList();

            item.PhysicalAddress1 = texts[0];

            string city = "";
            string state = "";
            string zip = "";

            Utils.GetCityStateZip(texts[1], ref city, ref state, ref zip, @"(.+)\s+([^\s]+)\s+([^\s]+)");

            item.PhysicalAddressCity = city;
            item.PhysicalAddressState = state;
            item.PhysicalAddressZip = zip;
        }

        private string GetSearchParameters(HtmlDocument doc, string parcelNumber, Item item) {           

            parcelNumber = parcelNumber.ToUpper().Replace("-", "");
            parcelNumber = string.Join("-", parcelNumber.Substring(0, 3), parcelNumber.Substring(3, 2), parcelNumber.Substring(5, 4));
            string[] splitted = parcelNumber.Split('-');

            string book = splitted[0];
            string map = splitted[1];
            string text = splitted[2];

            string parcel = string.Join("", text.Take(text.Length - 1).Select(x => x.ToString()).ToArray());
            string split = text[text.Length - 1].ToString();


            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ctl16", "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$up_Search|ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$btn_GoParcel");
            dict.Add("MSO_PageHashCode", GetValueOfInput(doc, "MSO_PageHashCode"));
            dict.Add("__SPSCEditMenu", "true");
            dict.Add("MSOWebPartPage_PostbackSource", "");
            dict.Add("MSOTlPn_SelectedWpId", "");
            dict.Add("MSOTlPn_View", "0");
            dict.Add("MSOTlPn_ShowSettings", "False");
            dict.Add("MSOGallery_SelectedLibrary", "");
            dict.Add("MSOGallery_FilterString", "");
            dict.Add("MSOTlPn_Button", "none");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__REQUESTDIGEST", GetValueOfInput(doc, "__REQUESTDIGEST"));
            dict.Add("MSOAuthoringConsole_FormContext", "");
            dict.Add("MSOAC_EditDuringWorkflow", "");
            dict.Add("MSOSPWebPartManager_DisplayModeName", "Browse");
            dict.Add("MSOWebPartPage_Shared", "");
            dict.Add("MSOLayout_LayoutChanges", "");
            dict.Add("MSOLayout_InDesignMode", "");
            dict.Add("MSOSPWebPartManager_OldDisplayModeName", "Browse");
            dict.Add("MSOSPWebPartManager_StartWebPartEditingName", "false");
            dict.Add("__LASTFOCUS", GetValueOfInput(doc, "__LASTFOCUS"));
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("Text1", " I'm looking for...");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$acc_Search_AccordionExtender_ClientState", "0");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Owner", item.MailingAddressOwner);
//?            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Owner", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Number", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Direction", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Name", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Suffix", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Book", book);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Map", map);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Parcel", parcel);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Split", split);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Section", "01");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Township", "01N");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Range", "02E");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Cabinet", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Slide", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Lot", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Subdivision", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear", GetValueOfInput(doc, "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear"));
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$btn_GoParcel", "Go");
            return WebQuery.GetStringFromParameters(dict);
        }


        private string GetSearchParametersAddress(HtmlDocument doc, String street, String HouseNum = "", String Direction = "", String Suffix = "")
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ctl16", "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$up_Search|ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$btn_GoPA");
            dict.Add("MSO_PageHashCode", GetValueOfInput(doc, "MSO_PageHashCode"));
            dict.Add("__SPSCEditMenu", "true");
            dict.Add("MSOWebPartPage_PostbackSource", "");
            dict.Add("MSOTlPn_SelectedWpId", "");
            dict.Add("MSOTlPn_View", "0");
            dict.Add("MSOTlPn_ShowSettings", "False");
            dict.Add("MSOGallery_SelectedLibrary", "");
            dict.Add("MSOGallery_FilterString", "");
            dict.Add("MSOTlPn_Button", "none");
            dict.Add("__EVENTTARGET", "");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__REQUESTDIGEST", GetValueOfInput(doc, "__REQUESTDIGEST"));
            dict.Add("MSOAuthoringConsole_FormContext", "");
            dict.Add("MSOAC_EditDuringWorkflow", "");
            dict.Add("MSOSPWebPartManager_DisplayModeName", "Browse");
            dict.Add("MSOWebPartPage_Shared", "");
            dict.Add("MSOLayout_LayoutChanges", "");
            dict.Add("MSOLayout_InDesignMode", "");
            dict.Add("MSOSPWebPartManager_OldDisplayModeName", "Browse");
            dict.Add("MSOSPWebPartManager_StartWebPartEditingName", "false");
            dict.Add("__LASTFOCUS", GetValueOfInput(doc, "__LASTFOCUS"));
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("__VIEWSTATEGENERATOR", GetValueOfInput(doc, "__VIEWSTATEGENERATOR"));
            dict.Add("Text1", " I'm looking for...");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$acc_Search_AccordionExtender_ClientState", "0");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Owner", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Number", HouseNum);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Direction", Direction);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Name", street);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Suffix", Suffix);
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Book", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Map", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Parcel", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Split", "0");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Section", "01");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Township", "01N");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$ddl_Range", "02E");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Cabinet", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Slide", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Lot", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$txt_Subdivision", "");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear", GetValueOfInput(doc, "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear"));
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl02$btn_GoPA", "Go");
            return WebQuery.GetStringFromParameters(dict);
        }

        private string GetSearchParameters2(HtmlDocument doc)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("ctl00$ctl16", "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$up_Search|ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl04$gv_Results$ctl02$lblTooltip");
            dict.Add("MSO_PageHashCode", GetValueOfInput(doc, "MSO_PageHashCode"));
            dict.Add("__SPSCEditMenu", "true");
            dict.Add("MSOWebPartPage_PostbackSource", "");
            dict.Add("MSOTlPn_SelectedWpId", "");
            dict.Add("MSOTlPn_View", "0");
            dict.Add("MSOTlPn_ShowSettings", "False");
            dict.Add("MSOGallery_SelectedLibrary", "");
            dict.Add("MSOGallery_FilterString", "");
            dict.Add("MSOTlPn_Button", "none");
            dict.Add("__EVENTTARGET", "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$ctl04$gv_Results$ctl02$lblTooltip");
            dict.Add("__EVENTARGUMENT", "");
            dict.Add("__REQUESTDIGEST", GetValueOfInput(doc, "__REQUESTDIGEST"));
            dict.Add("MSOAuthoringConsole_FormContext", "");
            dict.Add("MSOAC_EditDuringWorkflow", "");//
            dict.Add("MSOSPWebPartManager_DisplayModeName", "Browse");
            dict.Add("MSOWebPartPage_Shared", "");
            dict.Add("MSOLayout_LayoutChanges", "");
            dict.Add("MSOLayout_InDesignMode", "");
            dict.Add("MSOSPWebPartManager_OldDisplayModeName", "Browse");
            dict.Add("MSOSPWebPartManager_StartWebPartEditingName", "false");
            dict.Add("__LASTFOCUS", "");
            dict.Add("__VIEWSTATE", GetValueOfInput(doc, "__VIEWSTATE"));
            dict.Add("__VIEWSTATEGENERATOR", GetValueOfInput(doc, "__VIEWSTATEGENERATOR"));
            dict.Add("__VIEWSTATEENCRYPTED", "");
            dict.Add("__EVENTVALIDATION", GetValueOfInput(doc, "__EVENTVALIDATION"));
            dict.Add("Text1", " I'm looking for...");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$acc_Search_AccordionExtender_ClientState", "0");
            dict.Add("ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear", GetValueOfInput(doc, "ctl00$m$g_e7e24a7d_b298_4c40_8411_c65d5498a997$ctl00$hf_CurrentTaxYear"));
            return WebQuery.GetStringFromParameters(dict);
        }

        private HtmlDocument ProcessSelectingFirst(HtmlDocument doc) 
        {
            var nodes = doc.DocumentNode.SelectNodes("//table[contains(@id,'_gv_Results')]/tr/td/a");
            if (nodes != null && nodes.Count > 0)
            {
                var parameters = GetSearchParameters2(doc);
                InvokeSearching();
                return _webQuery.GetPost("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", parameters, 1, "http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", true);
            }
            return doc;
        }

        private string GetConstYear(HtmlDocument doc)
        {
            try
            {
                var nodes2 = doc.DocumentNode.SelectNodes("//strong[contains(text(),'Const')]");
                if (nodes2 != null)
                {
                    return WebQuery.Clean(nodes2[0].ParentNode.Sibling("td").SelectSingleNode("./span").InnerText);
                }
            }
            catch (Exception ex){
                Logger.Log("Error getting Const Year in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        /// <summary>
        /// Retrieve value for Item Tag
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private string GetPropertyType(HtmlDocument doc)
        {
            string result = "";
            try
            {
                var node = doc.DocumentNode.SelectSingleNode("//td[contains(text(),'Item:')]");
                if (node != null)
                {
                    string value = WebQuery.Clean(node.Sibling("td").InnerText);
                    if (!string.IsNullOrEmpty(value))
                        result = value;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Property Type in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return result;
        }

        private string GetTotalSqrFeet(HtmlDocument doc)
        {
            string result = "N/A";
            try
            {
                var node = doc.DocumentNode.SelectSingleNode("//td/span/strong[contains(text(),'Total Sq. Ft.:')]");
                if (node != null)
                {
                    string value = WebQuery.Clean(node.ParentNode.ParentNode.Sibling("td").ChildNodes[1].ChildNodes[1].InnerText);
                    if (!string.IsNullOrEmpty(value))
                        result = value;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Const Year in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return result;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValue(doc, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_LandLegalClass");

                return value.Contains("Owner Occupied") ? "Y" : "N";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";

        }

        private String GetResultSearch(String Street, String Number = "", String Direction = "", String Suffix = "")
        {
            var doc2 = _webQuery.GetSource("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", 1);
            var parameters = GetSearchParametersAddress(doc2, Street, Number, Direction, Suffix);
            doc2 = _webQuery.GetPost("http://pinalcountyaz.gov/Assessor/Pages/ParcelSearch.aspx", parameters, 1);
            var nodes = doc2.DocumentNode.SelectNodes("//table[contains(@id,'_gv_Results')]/tr/td/a");
            if (nodes != null && nodes.Count > 0)
            {
                var node = nodes.First().ParentNode.Sibling("td");
                return WebQuery.Clean(node.InnerText);
            }
            //doc2 = ProcessSelectingFirst(doc2);
            return GetValue(doc2, "ctl00_m_g_e7e24a7d_b298_4c40_8411_c65d5498a997_ctl00_ctl06_lbl_Owner1");
        }

        private void GetAdditionalOwmer(Item item)
        {
            var suffixes = new List<String>() { "AVE", "BND", "BLVD", "CIR", "CT", "DR", "HWY", "LN", "LOOP", "PASS", "PATH", "PKWY", "PL", "RD", "ST", "TRL", "WAY" };
            if (String.IsNullOrEmpty(item.MailingAddress)) return;
            try
            {
                var parts = Utils.SplitAddress(item.MailingAddress);
                var part1strs= parts[0].Split(' ').ToList();
                Int32 num = 0;
                if (Int32.TryParse(part1strs[0], out num)) part1strs.RemoveAt(0);
                var suffix = "";
                if (suffixes.Contains(part1strs.Last()))
                {
                    suffix = part1strs.Last();
                    part1strs.RemoveAt(part1strs.Count-1);
                }
                var direct = "";
                if (part1strs.Count > 1 && part1strs[0].Length == 1 && Utils._streetDirections.Contains(part1strs[0]))
                {
                    direct = part1strs[0];
                    part1strs.RemoveAt(0);
                }
                var street = String.Join(" ", part1strs);
                

                item.MailingAddressOwner = GetResultSearch(street, num.ToString(), direct, suffix);
            }
            catch { item.MailingAddressOwner = "n/a"; }
        }
    }
}
