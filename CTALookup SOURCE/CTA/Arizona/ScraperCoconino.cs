using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using CTALookup.Scrapers;
using System;
using System.Net;
using System.Globalization;

namespace CTALookup.Arizona
{
    public class ScraperCoconino : ScraperTylerTechnologies
    {
        public ScraperCoconino() : base("http://assessor.coconino.az.gov/") { }

        public override Scraper GetClone()
        {
            return new ScraperCoconino();
        }

        public override Item Scrape(string parcelNumber)
        {

            parcelNumber = parcelNumber.Replace("-", "");

            var item = new Item();

            // Create guest cookie.
            var parameters = new Dictionary<string, string>() { { "guest", "true" }, { "submit", "I Have Read The Above Statement" } };
            string par = WebQuery.GetStringFromParameters(parameters);
            var doc = _webQuery.GetPost("https://treasurer.coconino.az.gov/treasurer/web/loginPOST.jsp", par, 1);

            parameters = new Dictionary<string, string>() {
                {"TaxAccountID", ""}, { "TaxAOwnerIDSearchString", ""}, {"TaxAOwnerIDSearchType", "Standard Search" },
                {"TaxAParcelID", parcelNumber }, { "TaxDeactivatedID", "False"}
            };
            string searchParam = WebQuery.GetStringFromParameters(parameters);

            doc = _webQuery.GetPost("https://treasurer.coconino.az.gov/treasurer/treasurerweb/searchPOST.jsp", searchParam, 1);

            if (doc.DocumentNode.OuterHtml.Contains("No accounts found"))
            {
                return item;
            }

            
            var aNodes = doc.DocumentNode.SelectNodes("//table[@id='searchResultsTable']//a[contains(@href, 'account.jsp?account=')]");            
            /*if (aNodes.Count > 2) {
                Logger.Log(string.Format("Mailing Address won't be obtained for parcel {0} because multiple results appeared", parcelNumber));
                return item;
            }*/
            //var found = false;
            //foreach (var nn in aNodes)
            //{
            //    if (!nn.InnerText.Contains("R")) continue;
            //    string url = WebQuery.BuildUrl(WebQuery.Clean(nn.Attributes["href"].Value),
            //        "http://treasurer.coconino.az.gov:81/treasurer/treasurerweb/");
            //    doc = _webQuery.GetSource(url, 1);
            //    found = true;
            //    break;
            //}
            //if (!found) return item;
            //var item2 = new Item();
            string accountNumber = "";
            foreach (var aNode in aNodes)
            {
                int index = aNode.Attributes["href"].Value.IndexOf("=");
                accountNumber = aNode.Attributes["href"].Value.Substring(index + 1);
                if (accountNumber.StartsWith("R"))
                {
                    break;
                }
            }
            if (string.IsNullOrEmpty(accountNumber))
            {
                return item;
            }       

            // Simulate guest login
            parameters = new Dictionary<string, string>() { { "guest", "true" }, { "submit", "Enter+EagleWeb" } };
            par = WebQuery.GetStringFromParameters(parameters);
            doc = _webQuery.GetPost("http://assessor.coconino.az.gov:82/assessor/web/loginPOST.jsp?guest=true&submit=Enter+EagleWeb", par, 1);
            //http://assessor.coconino.az.gov:82/assessor/taxweb/account.jsp?accountNum=R0030888
            //http://assessor.coconino.az.gov:82/assessor/taxweb/results.jsp
            //doc = _webQuery.GetPost("http://assessor.coconino.az.gov:82/assessor/web/loginPOST.jsp?guest=true&submit=Enter+EagleWeb", par, 1);

            //ParcelNumberID
            //http://assessor.coconino.az.gov:82/assessor/taxweb/results.jsp
             parameters = new Dictionary<string, string>() { { "AccountNumID", accountNumber } };
            //http://assessor.coconino.az.gov:82/assessor/taxweb/account.jsp?accountNum=R0030888
            doc = _webQuery.GetSource(string.Format("http://assessor.coconino.az.gov:82/assessor/taxweb/results.jsp?AccountNumID={0}", accountNumber), 1);

            // Get assessor page
            //doc = _webQuery.GetSource(string.Format("http://assessor.coconino.az.gov:82/assessor/taxweb/account.jsp?accountNum={0}", accountNumber), 1);
            //doc = _webQuery.GetSource(string.Format("http://assessor.coconino.az.gov:82/assessor/taxweb/account.jsp?accountNum={0}", accountNumber), 1);

            var node = doc.DocumentNode.SelectSingleNode("//td[contains(b/text(), 'Full Cash Value (FCV)')]");
            if(node!=null)
            
            item.MarketValue = node.SelectSingleNode("td").InnerText;
            node = doc.DocumentNode.SelectSingleNode("//td/b[contains(text(), 'Owner Address')]");
            var nodes = doc.DocumentNode.SelectNodes("//*[@id='middle']/table/tbody/tr[2]/td[2]");
            ////*[@id="middle"]/table/tbody/tr[2]/td[2]/table/tbody/tr[3]/td
            string a = node.ParentNode.InnerHtml;
            string[] add = a.Split(new string[] { "<br>" }, StringSplitOptions.None);
            item.OwnerAddress = add[0].Replace("<b>Owner Address</b>", "").Trim();
            string[] cityStateZip = add[1].Split(',');
            item.OwnerCity = cityStateZip[0].Trim();
            string stateZip = cityStateZip[1].Trim();
            item.OwnerState = stateZip.Split(' ')[0].Trim();
            item.OwnerZip = stateZip.Split(' ')[1].Trim();

 //?           Utils.AssignFullAddrToOwnerAddress(item, node.ParentNode.InnerText.Replace("Owner Address", ""), regex: @"(?<state>.+) (?<zip>.+)");
 //?           item.OwnerAddress = node.ParentNode.InnerText.Replace("Owner Address", "");

            node = doc.DocumentNode.SelectSingleNode("//table[@id='columns']//a[text()='Parcel Detail']");
            if(node != null)
            {
                string parcelDetailLink = node.GetAttributeValue("href", "");
                //Parcel Detail
                doc = _webQuery.GetSource(string.Format("http://assessor.coconino.az.gov:82/assessor/taxweb/{0}", parcelDetailLink), 1);
                node = doc.DocumentNode.SelectSingleNode("//td/span[text()='Parcel Size']");
                if(node != null)
                    item.Acreage = WebQuery.Clean(node.Sibling("span").InnerText);

                node = doc.DocumentNode.SelectSingleNode("//td/span[text()='City']");
                if(node != null)
                    item.PhysicalAddressCity = WebQuery.Clean(node.Sibling("span").InnerText);
                item.PhysicalAddressState = "AZ";
            }            
            node = doc.DocumentNode.SelectSingleNode("//table[@id='columns']//a[text()='Land']");
            if(node != null)
            {
                string landLink = node.GetAttributeValue("href", "");
                //Land Detail
                doc = _webQuery.GetSource(string.Format("http://assessor.coconino.az.gov:82/assessor/taxweb/{0}", landLink), 1);
                node = doc.DocumentNode.SelectSingleNode("//td/span[text()='Property Code']");
                item.PropertyType = WebQuery.Clean(node.Sibling("span").InnerText);
            }             

            AssignPriorYearLien(item, accountNumber);

            doc = _webQuery.GetSource(string.Format("https://treasurer.coconino.az.gov/treasurer/treasurerweb/account.jsp?account={0}", accountNumber), 1);

            item.MapNumber = GetValue(doc, "Parcel");
            item.PhysicalAddress1 = GetValue(doc, "Situs");
            item.LegalDescription = GetValue(doc, "Legal");

            if (item.LegalDescription.ToLowerInvariant().IndexOf("mbl home") > 0)
                item.SetReasonToOmit("Mobile home");

            var str = GetValue(doc, "Owners");
            var tmpstr = item.OwnerName;
            item.OwnerName = item.OwnerFirstName = item.OwnerLastName = item.OwnerMiddleInitial = item.Company = "";
            AssignNames(item, str);

            var tdNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'Address')]");
            if (tdNode == null)
            {
                Logger.Log(string.Format("No 'Address' field found for parcel {0}", parcelNumber));
                return item;
            }

            var td = tdNode.Sibling("td");

            var texts =
                td.SelectNodes("./text()")
                    .Select(x => WebQuery.Clean(x.InnerText))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

            item.MailingAddressOwner = item.OwnerName;
            item.MailingAddress = texts[0];

            try
            {

                AssignCityStateZipToMailingAddress(item, texts[1], @"(.+),\s*(.+)\s+(.+)");
            }
            catch
            {
                item.MailingCity = String.Join(" ", texts.Skip(1));
            }
            return item;
        }

        private void AssignPriorYearLien(Item item, string accountNumber)
        {
            //Tax History column:0 year, column:1 type, column2:date, column3:fee
            // Tuple: Item1-Date, Item2-Type, Item3-Fee
            HtmlDocument doc = _webQuery.GetSource(string.Format("https://treasurer.coconino.az.gov/treasurer/treasurerweb/account.jsp?account={0}&action=tx", accountNumber), 1);
            var tableRows = doc.DocumentNode.SelectNodes("//table[@class='account stripe']/tbody/tr");
            if (tableRows != null)
            {
                var list = from row in tableRows
                           orderby DateTime.ParseExact(WebQuery.Clean(row.ChildNodes[2].InnerText), "M/d/yy", CultureInfo.InvariantCulture)
                           where WebQuery.Clean(row.ChildNodes[1].InnerText).Equals("Redemption Fee") ||
                                    (WebQuery.Clean(row.ChildNodes[1].InnerText).StartsWith("Misc Charge") &&
                                     WebQuery.Clean(row.ChildNodes[3].InnerText).Equals("$25.00"))
                           //group row by WebQuery.Clean(row.ChildNodes[0].InnerText) into g
                           select new TaxDetail()
                           {
                               //Year = g.Key,
                               //Details = g.ToList().Select<HtmlNode, TaxDetail>((n) => new TaxDetail()
                               //{
                               Date = DateTime.ParseExact(WebQuery.Clean(row.ChildNodes[2].InnerText), "M/d/yy", CultureInfo.InvariantCulture),
                               Type = WebQuery.Clean(row.ChildNodes[1].InnerText),
                               Fee = WebQuery.Clean(row.ChildNodes[3].InnerText)
                               //})
                           };
                bool priorLiens = list.Where((tax) => tax.Type.StartsWith("Misc Charge"))
                    .Any((miscCharge)=>!list.Any((redemption)=>
                    {
                        return redemption.Type.Equals("Redemption Fee") && redemption.Date >= miscCharge.Date;
                    }));

                if (priorLiens)
                {
                    item.Description = "PRIOR YEAR LIENS";
                    item.SetReasonToOmit(item.Description);
                }

                //foreach (var taxYear in list)
                //{
                //    var redemptionFee = taxYear.Details.SingleOrDefault((detail) => detail.Type.Equals("Redemption Fee"));
                //    if (redemptionFee != null)
                //    {
                //        bool exists = taxYear.Details.Any((detail) => detail.Type.StartsWith("Misc Charge") && detail.Date > redemptionFee.Date);
                //        if (exists)
                //        {
                //            item.Description = "PRIOR YEAR LIENS";
                //            break;
                //        }
                //    }
                //}
            }
        }

        private string GetValue(HtmlDocument doc, String Caption)
        {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//td[contains(text(), '{0}') or contains(a/text(), '{0}')]", Caption));
            if (node == null) return "";
            //var tdNode = node.Ancestors("td").First();

            var tdNode = node.Sibling("td");//tdNode.Sibling("td");

            return WebQuery.Clean(tdNode.InnerText);
        }

        private string GetMailingAddressParameters(HtmlDocument doc, string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("TaxAccountID", "");
            dict.Add("TaxAOwnerIDSearchString", "");
            dict.Add("TaxAOwnerIDSearchType", "Standard Search");
            dict.Add("TaxAParcelID", parcelNumber);
            return WebQuery.GetStringFromParameters(dict);
        }

        public override bool CanScrape(string county)
        {
            return county.ToLower() == "arizona:coconino";
        }

        class TaxYear
        {
            public string Year { get; set; }
            public IEnumerable<TaxDetail> Details { get; set; }
        }

        class TaxDetail
        {
            public DateTime Date { get; set; }
            public string Fee { get; set; }
            public string Type { get; set; }
        }
    }
}
