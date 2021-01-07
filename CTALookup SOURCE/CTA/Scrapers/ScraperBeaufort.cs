using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperBeaufort : Scraper
    {
        private const string BaseUrl = "http://sc-beaufort-county.governmax.com/svc/";
        
        private const string SearchUrl = "http://sc-beaufort-county.governmax.com/svc/propertymax/search_property.asp?go.x=1";
        public override bool CanScrape(string county) {
            return county == "South Carolina:Beaufort";
        }

        public override Scraper GetClone()
        {
            return new ScraperBeaufort();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            InvokeOpeningUrl(BaseUrl);
            var doc = _webQuery.GetSource(BaseUrl, 1);

            var sid = GetSID(doc);
            
            string parameters = "";
            try
            {
                parameters = GetSearchParameters(sid, parcelNumber);
            }
            catch (Exception ex)
            {
                LogThrowErrorGettingParams(doc, ex);
            }

            InvokeSearching();
            doc = _webQuery.GetPost(SearchUrl, parameters, 1);
            //doc.Load(SearchUrl + parameters);
            //If we need to click 
            //if (doc.DocumentNode.OuterHtml.Contains("Property ID (PIN)"))
            //{
            //    var link = GetDetailsLink(doc);
            //    doc = _webQuery.GetSource(link, 1);
            //}

            //if (doc.DocumentNode.OuterHtml.Contains("No Records Found"))
            //{
            //    InvokeNoItemsFound();
            //    return null;
            //}

            return GetItem(doc,parcelNumber);
        }

        private string GetDetailsLink(HtmlDocument doc) {
            var node = doc.DocumentNode.SelectSingleNode("//a[@class='listlink' and contains(@href, 'sc-beaufort')]");
            if (node == null) {
                Logger.LogMessageCodeAndThrowException("Error getting link of details page from current document", doc.DocumentNode.OuterHtml);
            }
            var link = WebQuery.Clean(node.Attributes["href"].Value);
            link = link.Replace("../../", "");
            return WebQuery.BuildUrl(link, "http://sc-beaufort-county.governmax.com/svc/");
        }

        public Item GetItem(HtmlDocument doc,string parcel, bool openExternalLinks = true) {
            var item = new Item();
            try {

                //HtmlNodeCollection tablevalues = doc.DocumentNode.SelectNodes("//td[.//text()[contains(., 'Property ID (PIN)')]]");
                HtmlNodeCollection tdowner = doc.DocumentNode.SelectNodes("//td[.//text()[contains(., 'Owner')]]");
                //HtmlNode ndoe = tdowner[7].SelectSingleNode("/following-sibling::td");
                HtmlNodeCollection ownerdetail = tdowner[6].SelectNodes(".//td");
                string name = ownerdetail[1].InnerText.Trim();
                AssignNames(item, name);
                item.MapNumber = parcel;
                HtmlNodeCollection parceladdress = doc.DocumentNode.SelectNodes("//table[.//text()[contains(., 'Parcel Address')]]");
                HtmlNodeCollection ndoe1 = parceladdress[2].SelectNodes(".//td");
                item.PhysicalAddress1 = ndoe1[8].InnerText.Trim();
                if (item.PhysicalAddress1 != "")
                {
                    string[] listdata = item.PhysicalAddress1.Trim().Split(',');
                    if (listdata.Length == 2)
                    {
                        item.PhysicalAddress1 = listdata[0];
                        item.PhysicalAddressCity = listdata[1];
                    }
                }
                if (ownerdetail[3].InnerText.Trim() != "")
                {
                    string[] listdata = ownerdetail[3].InnerHtml.ToString().Trim().Split('\n');
                    if (listdata.Length >=3)
                    {
                        listdata = listdata[1].Split(new string[]{"<br>"}, StringSplitOptions.RemoveEmptyEntries);
                        if (listdata.Length > 1)
                        {
                            item.OwnerAddress = listdata[0].Trim();
                            listdata = listdata[1].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        }

                        item.OwnerZip = listdata[listdata.Length - 1];
                        item.OwnerState = listdata[listdata.Length - 2];
                        for (int i = listdata.Length - 3; i >=0; i--)
                        {
                            item.OwnerCity = listdata[i] + " " + item.OwnerCity;
                        }
                        
                    }
                    
                    
                    
                }

                HtmlNodeCollection legaldescs = doc.DocumentNode.SelectNodes("//td[.//text()[contains(., 'Description')]]");
                HtmlNode legaldesc = legaldescs[7].SelectSingleNode(".//following-sibling::td");
                //item.MapNumber =
                //    WebQuery.Clean(
                //        doc.DocumentNode.SelectSingleNode("//table[@cellspacing='2']//tr[2]/td[1]//span").InnerText);

                item.LegalDescription = legaldesc.InnerText.Trim();
                HtmlNodeCollection Acreagelist = doc.DocumentNode.SelectNodes("//td[.//text()[contains(., 'Acreage')]]");
                HtmlNode acreage = Acreagelist[4].SelectSingleNode(".//following-sibling::td");

                item.Acreage = acreage.InnerText.Trim();

                //item.PhysicalAddress1 = item.PhysicalAddress1.Replace(item.OwnerZip, "").Trim();
                //item.PhysicalAddress1 = item.PhysicalAddress1.Replace(item.OwnerState, "").Trim();
                //item.PhysicalAddress1 = item.PhysicalAddress1.Replace(item.OwnerCity, "").Trim();
               
                var table = GetHistoricInfoTable(doc);

                var rows = table.SelectNodes(".//tr").Skip(1).ToList();

                var cleanedData = GetCleanRows(rows);

                item.LandValue = GetValueFromCleanedData(cleanedData, 1);
                item.ImprovementValue = GetValueFromCleanedData(cleanedData, 2);
                item.MarketValue = GetValueFromCleanedData(cleanedData, 3);

                if (openExternalLinks) {
                   var url =
                        WebQuery.Clean(doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Parcel')]").Attributes["href"].Value);
                   InvokeOpeningUrl(url);
                    doc = _webQuery.GetSource(url, 1);

                    try { item.OwnerResident = GetOwnerName(doc); } catch {}
                    item.WaterfrontPropertyType = GetWaterfrontPropertyType(doc);
                }

            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        public string GetWaterfrontPropertyType(HtmlDocument doc) {
            return WebQuery.Clean(GetValueOfField(doc, "Waterfront"));
        }

        private string GetValueFromCleanedData(IList<string[]> cleanedData, int fieldIndex) {
            foreach (var x in cleanedData) {
                if (!string.IsNullOrEmpty(x[fieldIndex])) {
                    return x[fieldIndex];
                }
            }
            return "";
        }

        private IList<string[]> GetCleanRows(List<HtmlNode> rows) {
            IList<string[]> result = new List<string[]>();
            foreach (var row in rows) {
                string[] r = new string[6];
                var cells = row.SelectNodes("./td");
                for (int i = 0; i < Math.Min(cells.Count, 6); i++) {
                    var cell = cells[i];
                    r[i] = WebQuery.Clean(cell.InnerText);
                }
                result.Add(r);
            }
            return result;
        }

        private HtmlNode GetHistoricInfoTable(HtmlDocument doc) {
            var node = doc.DocumentNode.SelectSingleNode("//span[contains(text(), 'Historic Information')]");
            var table = node.Ancestors("table").First();

            return table.NextSibling;

        }

        private static string GetValueOfField(HtmlDocument doc, string fieldName)
        {
            var nodes = GetNodeOfField(doc, fieldName);
            if (nodes == null)
            {
                LogMessageCodeAndThrowException(string.Format("Error getting value of field {0}", fieldName), doc.DocumentNode.OuterHtml);
            }
            return WebQuery.Clean(nodes[0].ParentNode.ParentNode.NextSibling.NextSibling.InnerText);
        }

        private static HtmlNodeCollection GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            return doc.DocumentNode.SelectNodes(string.Format("//span[@class='datalabel' and contains(text(), '{0}')]", fieldName));
        }

        private string GetSearchParameters(string sid, string parcelNumber) {
            var dict = new Dictionary<string, string>();
            //t_nm=summary&l_cr=1&parcelid=R800+022+00B+0123+0000&sid=6655C9E95FFE42CC808A0D962706C5FC
            dict.Add("p.parcelid", parcelNumber);
            dict.Add("go", "  Go  ");
            dict.Add("site", "propertysearch");
            dict.Add("l_nm", "parcelid");
            dict.Add("sid", sid);
            //dict.Add("t_nm", "summary");
            //dict.Add("l_cr", "1");
            //dict.Add("parcelid", parcelNumber);
            
            //dict.Add("sid", sid);
            //dict.Add("t_nm", "summary");
            //dict.Add("l_cr", "1");
            //dict.Add("parcelid", parcelNumber);
            return WebQuery.GetStringFromParameters(dict);
        }

        public string GetSID(HtmlDocument doc) {
            var m = Regex.Match(doc.DocumentNode.OuterHtml, @"sid=(.+)?&");
            if (!m.Success) {
                Logger.LogMessageCodeAndThrowException("Error parsing SID from current document", doc.DocumentNode.OuterHtml);
            }
            return WebQuery.Clean(m.Groups[1].Value);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            try
            {
                var value = GetValueOfField(doc, "Exemption Amount");
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
    }
}
