using System;
using System.Collections.Generic;
using System.Linq;
using CTALookup.Scrapers;
using CTALookup.ViewModel;
using HtmlAgilityPack;

namespace CTALookup.Georgia {
    public class ScraperGeorgiaNew : ScraperQpublic {
        private readonly string _county;

        public ScraperGeorgiaNew(string county, string countyCode, string searchUrl,
            string submitUrl = "http://qpublic7.qpublic.net/ga_alsearch.php",
            string xpathLinkNodes = "//table[@class='table_class']//tr[3]",
            string baseUrl = "https://qpublic.schneidercorp.com/") {
            _county = county;
            CountyCode = countyCode;
            SearchUrl = searchUrl;
            SubmitUrl = submitUrl;
            XpathLinkNodes = xpathLinkNodes;
            BaseUrl = baseUrl;
        }

        public override bool CanScrape(string county) {
            return county == "Georgia:" + _county;
        }

        public override Scraper GetClone()
        {
            return new ScraperGeorgia(_county, CountyCode, SearchUrl, SubmitUrl, XpathLinkNodes, BaseUrl);
        }

        public override Item Scrape(string parcelNumber) {
            var doc = Search(parcelNumber);
            if (NoRecordsFound(doc)) {
                InvokeNoItemsFound();
                return null;
            }

            string link = GetLink(doc);
            InvokeOpeningUrl(link);
            Item item = null;
            try {
                item = GetItem(link);
                if (item.MapNumber == "n/a") {
                    item.MapNumber = parcelNumber;
                }
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            try
            {
                var value = GetValueOfField(doc, "Homestead");
                if (value == "n/a") {
                    return "n/a";
                }
                return value.StartsWith("Yes") ? "Y" : "N";
            }
            catch (Exception ex)
            {
                Logger.Log("Error getting Owner Resident in current document");
                Logger.LogException(ex);
                Logger.LogCode(doc.DocumentNode.OuterHtml);
            }
            return "";
        }

        /*public string GetVerticalValueFromQpublic(HtmlDocument doc, params string[] fieldNames) {
            HtmlNode tdNode = null;

            doc.OptionAutoCloseOnEnd = true;

            IList<string> fields = fieldNames.ToList();
            foreach (var f in fieldNames) {
                fields.Insert(0, f.Replace(" ", ""));
            }
            
            foreach (var fieldName in fields) {
                tdNode =
                    doc.DocumentNode.SelectNodes("//td")
                        .FirstOrDefault(
                            x => x.SelectSingleNode(".//td") == null && WebQuery.Clean(x.InnerText).ToLower().Contains(fieldName.ToLower()));
                if (tdNode != null) {
                    break;
                }
            }

            if (tdNode == null) {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            int index = trNode.SelectNodes("./td").IndexOf(tdNode);

            if (trNode.NextSibling == null)
            {
                trNode = trNode.SelectSingleNode("./tr");
            }
            else
            {

                trNode = trNode.NextSibling;


                while (trNode.Name.ToLower() != "tr")
                {
                    trNode = trNode.NextSibling;
                }
            }

            tdNode = trNode.SelectNodes("./td")[index];

            string value = WebQuery.Clean(tdNode.InnerText);

            return System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ");
        }*/

        private Item GetItem(string link) {
            var doc = _webQuery.GetSource(link, 1);
            Logger.Log(doc.DocumentNode.InnerHtml);
            var nextLinkNode =
                doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Click Here To Continue To Property')]");
            if (nextLinkNode != null) {
                string url = WebQuery.BuildUrl(WebQuery.Clean(nextLinkNode.Attributes["href"].Value), BaseUrl);
                doc = _webQuery.GetSource(url, 1);
            }

            var item = new Item();
            item.MapNumber = GetValueOfField(doc, "Parcel Number");
            item.LegalDescription = GetValueOfField(doc, "Legal Description");
            item.Acreage = GetValueOfField(doc, "Acres");
            item.PhysicalAddress1 = GetValueOfField(doc, "Location Address");
            string name = GetValueOfField(doc, "Owner Name");
            AssignNames(item, name);

            AssignOwnerAddress(item, doc);
            
            item.MarketValue = GetVerticalValueFromQpublic(doc, "Total Value", "Market Value");
            item.LandValue = GetVerticalValueFromQpublic(doc, "Land Value");
            item.AccessoryValue = GetVerticalValueFromQpublic(doc, "Accessory Value", "Misc");//john
            item.ImprovementValue = GetVerticalValueFromQpublic(doc, "Improvement Value", "Building Value");
            item.OwnerResident = GetOwnerName(doc);
            
            //var str = GetVerticalValueFromQpublic(doc, "Sq Ft", "Square Feet");
            var str = GetVerticalValueFromQpublic(doc, "Basement Area Sq Ft", "Square Feet");
            if (str == "n/a" || string.IsNullOrWhiteSpace(str))
            {
                str = GetVerticalValueFromQpublic(doc, @"Heated");
            }
            AssignAdditionalNote(doc, item, str, "sqft");
            str = GetVerticalValueFromQpublic(doc, @"Bedrooms");
            AssignAdditionalNote(doc, item, str, "R/BR/BA/ExP");
            var eyearb = GetVerticalValueFromQpublic(doc, "Effective Year Built");
            var eyear = GetVerticalValueFromQpublic(doc, "Year Built");
            if(!string.IsNullOrWhiteSpace(eyearb) && eyearb!="n/a" && eyearb!="0")
                AssignAdditionalNote(doc, item, eyearb, "YR");
            else if (!string.IsNullOrWhiteSpace(eyear) && eyear != "n/a" && eyear != "0")
                AssignAdditionalNote(doc, item, eyear, "YR");

            str = GetVerticalValueFromQpublic(doc, "Condition", "Cond");
            AssignAdditionalNote(doc, item, str, "condition");

            var rows = doc.DocumentNode.SelectNodes("//table[contains(tr/td/text(), 'Accessory Information')]/tr");
            if (rows != null && rows.Count > 0 && !(rows.Count==3 && rows.Any(r => r.InnerHtml.Contains("No accessory information"))))
                for (Int32 i = 2; i < rows.Count; ++i)
                {
                    str = String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine;
                    var cells = rows[i].SelectNodes("td/text()");
                    for (Int32 j = 0; j < cells.Count; ++j )
                    { 
                        if (j == cells.Count -2) continue;
                        if (j != 0) str += ", ";
                        str += WebQuery.Clean(cells[j].InnerText).Replace("  ", "");
                    }
                    AssignAdditionalNote(doc, item, str, (String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine )+"Accessory " + (i - 1).ToString());
                }

            rows = doc.DocumentNode.SelectNodes("//table[contains(tr/td/text(), 'Sale Information')]/tr");
            if (rows != null && rows.Count > 0 && !(rows.Count == 3 && rows.Any(r => r.InnerHtml.Contains("No sales information"))))
            {
                var cols = rows[1].SelectNodes("td").Select(r => WebQuery.Clean(r.InnerText)).ToList();
                cols[1] = "DB";
                for (Int32 i = 2; i < rows.Count; ++i)
                {
                    str = "";
                    var cells = rows[i].SelectNodes("td");
                    for (Int32 j = 0; j < cells.Count; ++j)
                    {
                        if (j == 2) continue;
                        if (j != 0) str += ", ";
                        if (j != 0 && j != 3) str += cols[j] + ":";
                        var tmpstr = WebQuery.Clean(cells[j].InnerText);
                        if (j == 3) tmpstr = tmpstr.Replace(" ", "");
                        str += tmpstr;
                    }
                    AssignAdditionalNote(doc, item, str, (String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine) + "Sale " + (i - 1).ToString());
                }
            }
            item.Images = GetImages(doc);
            return item;
        }

        public IList<string> GetParcelSamples(string name)
        {
            return GetParcelSamples(name,false,"");
        }

        public IList<string> GetParcelSamples(string name,bool globalSearch, string county) {
            ResetWebQuery();
            
            //Search by name instead
            string searchUrl = GetSearchOwnerUrl();
            
            var doc = _webQuery.GetSource(searchUrl, 1);

            if(!globalSearch)
                InvokeSearching();

            string parameters =globalSearch? GetSearchOwnerParameters(name,county) : GetSearchOwnerParameters(name);
            if (!String.IsNullOrEmpty(parameters))
            {
                if (!globalSearch)
                    InvokeSearching();
                doc = _webQuery.GetPost(SubmitUrl, parameters, 1);
            }

            if (doc.DocumentNode.OuterHtml.Contains("must be >= 2")) {
                return GetParcelSamples("BLACK");
            }
            
            //Parse parcels
            IList<string> parcels = GetParcels(doc);

            return parcels.Take(30).ToList();
        }

        public void TestGeorgia()
        {
            var name = "B";
            var parcelList = new List<Tuple<string, string>>();
            foreach (var county in ContentViewModel.GeorgiaCounties)
            {
                ResetWebQuery();

                //Search by name instead
                string searchUrl = GetSearchOwnerUrl();

                var doc = _webQuery.GetSource(searchUrl, 1);

                InvokeSearching();

                string parameters = GetSearchOwnerParameters(name,county.Key);
                if (!String.IsNullOrEmpty(parameters))
                {
                    InvokeSearching();
                    doc = _webQuery.GetPost(SubmitUrl, parameters, 1);
                }

                if (doc.DocumentNode.OuterHtml.Contains("must be >= 2"))
                {
                    continue;
                    //return GetParcelSamples("BLACK");
                }

                //Parse parcels
                IList<string> parcels = GetParcels(doc);
                var rand = new Random();
                var item = parcels[rand.Next(parcels.Count)];
                parcelList.Add(Tuple.Create(county.Key, item));
                item = parcels[rand.Next(parcels.Count)];
                parcelList.Add(Tuple.Create(county.Key, item));

            }


            foreach (var item in parcelList)
            {
                
            }
        }

        protected virtual string GetSearchOwnerUrl() {
            return SearchUrl.Replace("search=parcel", "search=name");
        }

        protected virtual IList<string> GetParcels(HtmlDocument doc) {
            var nodes = doc.DocumentNode.SelectNodes("//td[@class='search_value'][1]/a");
            if (nodes == null)
            {
                return new List<string> { "An error ocurred" };
            }

            return nodes.Select(x => WebQuery.Clean(x.InnerText)).ToList();
        }

        protected virtual string GetSearchOwnerParameters(string name) {
            var dict = new Dictionary<string, string>
            {
                {"BEGIN", "0"},
                {"INPUT", name},
                {"searchType", "owner_name"},
                {"county", CountyCode},
                {"Owner_Search", "Search By Owner Name"}
            };
            return WebQuery.GetStringFromParameters(dict);
        }

        protected virtual string GetSearchOwnerParameters(string name,string county) {
            var dict = new Dictionary<string, string>
            {
                {"BEGIN", "0"},
                {"INPUT", name},
                {"searchType", "owner_name"},
                {"county",county},
                {"Owner_Search", "Search By Owner Name"}
            };
            return WebQuery.GetStringFromParameters(dict);
        }

        private void AssignAdditionalNote(HtmlDocument doc, Item item, String value, String Abbr, Func<String, Boolean> FuncYesNo = null)
        {
            if (String.IsNullOrEmpty(value) || value == "n/a") return;
            if (FuncYesNo != null) value = FuncYesNo(value) ? "Y" : "N";
            item.Notes += String.Format("    {0}: {1}", Abbr, value);
        }
    }
}