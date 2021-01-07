using System;
using System.Collections.Generic;
using System.Drawing;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Arizona
{
    /// <summary>
    /// Scrapes sites created with Tyler Technologies (See bottom of the page)
    /// </summary>
    public abstract class ScraperTylerTechnologies : ScraperArizona
    {
        protected ScraperTylerTechnologies(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        protected string BaseUrl { get; set; }

        public override Item Scrape(string parcelNumber)
        {
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource(BaseUrl + "assessor/web/loginPOST.jsp?submit=Login&guest=true", 1);

            string parameters = GetSearchParameters(doc, parcelNumber);

            InvokeSearching();
            doc = _webQuery.GetPost(BaseUrl + "assessor/taxweb/results.jsp", parameters, 1);

            var nodes = doc.DocumentNode.SelectNodes("//td/a[contains(@href, 'account.jsp')]");
            if (nodes == null) return null;
            var aNode = nodes[0];

            string url = BaseUrl + "assessor/taxweb/" + aNode.Attributes["href"].Value;

            InvokeOpeningUrl(url);
            doc = _webQuery.GetSource(url, 1);

            var item = GetItem(doc);

            if (nodes.Count > 3)
            {
                item.MultipleColumns = "Y";
            }

            return item;
        }

        private Item GetItem(HtmlDocument doc)
        {
            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfField(doc, "Parcel Number");
                item.LegalDescription = GetValueOfField(doc, "Legal Summary");
                item.Acreage = GetValueOfField(doc, "Parcel Size");
                item.PhysicalAddress1 = GetValueOfField(doc, "Situs Address");
                item.PhysicalAddressCity = GetValueOfField(doc, "City");
                item.PhysicalAddressState = "AZ";

                AssignOwnerName(doc, item);

                AssignOwnerAddress(doc, item);

                item.MarketValue = GetMarketValue(doc);
                item.Image = GetImage(doc);

                var node = doc.DocumentNode.SelectSingleNode("//table//tr[contains(th/*/text(), 'Legal Class') or contains(th/text(), 'Legal Class') ]");
                if (node != null)
                {
                    node = node.Sibling("tr").SelectSingleNode("td");
                    item.PropertyType = WebQuery.Clean(node.InnerText);
                    item.PropertyType = item.PropertyType.Split('.')[0];
                    Int32 res;
                    if (Int32.TryParse(item.PropertyType, out res))
                    {
                        switch (res)
                        {
                            case 1: item.PropertyType = "Commercial"; break;
                            case 2: item.PropertyType = "Vacant Land, Agricultural, golf courses"; break;
                            case 3: item.PropertyType = "Owner occupied residential"; break;
                            case 4: item.PropertyType = "Residential Rental, REO"; break;
                            case 5: item.PropertyType = "Railroad"; break;
                            case 6: item.PropertyType = "Historical residential, foreign trade zone"; break;
                            case 7: item.PropertyType = "Historic commercial and industrial"; break;
                            case 8: item.PropertyType = "Renovated historic"; break;
                            case 9: item.PropertyType = "Certain improvements on government property"; break;
                            default: break;
                        }
                    }
                }
                node = doc.DocumentNode.SelectSingleNode("//div[@id='left']/a[contains(text(), 'Mobile')]");
                if (node != null)
                    item.PropertyType = "MOBILE " + item.PropertyType;


                node = doc.DocumentNode.SelectSingleNode("//div[@id='left']/a[contains(text(), 'Residential')]");
                if (node != null)
                {
                    var str = BaseUrl + "assessor/taxweb/" + node.GetAttributeValue("href", "");
                    doc = _webQuery.GetSource(str, 1);
                    item.PropertyType += ", " + GetVerticalValue(doc, "Percent") + "%" + " | " + GetVerticalValue(doc, "Property Code");
                    item.Acreage = GetVerticalValue(doc, "Acres");
                    AssignAdditionalNote(item, "SQFT", GetVerticalValue(doc, "SQFT"));
                    AssignAdditionalNote(item, "YR", GetVerticalValue(doc, "Actual Year"));
                }

                node = doc.DocumentNode.SelectSingleNode("//div[@id='left']/a[contains(text(), 'Assessment History')]");
                if (node != null)
                {
                    var str = BaseUrl + "assessor/taxweb/" + node.GetAttributeValue("href", "");
                    doc = _webQuery.GetSource(str, 1);
                    node = doc.DocumentNode.SelectSingleNode("//table[@class='tableHtmlLayout']//tr/td[contains(*/text(),'Land') or contains(text(),'Land')]");
                    node = node.Sibling("td");
                    item.LandValue = WebQuery.Clean(node.InnerText);

                    node = doc.DocumentNode.SelectSingleNode("//table[@class='tableHtmlLayout']//tr/td[contains(*/text(),'Improvement') or contains(text(),'Improvement')]");
                    node = node.Sibling("td");
                    item.ImprovementValue = WebQuery.Clean(node.InnerText);
                }
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private void AssignAdditionalNote(Item item, String Caption, String Value, String Suffix = "")
        {
            if (!String.IsNullOrEmpty(Value) && Value != "n/a")
                item.Notes += String.Format("{0}{1}: {2}{3}", String.IsNullOrEmpty(item.Notes) ? "" : "    ", Caption, Value, Suffix);
        }

        private String GetVerticalValue(HtmlDocument doc, String FieldName)
        {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//span[@class='fieldLabel' and contains(text(), '{0}')]", FieldName));
            if (node == null) return "";
            node = node.Sibling("span");
            return WebQuery.Clean(node.InnerText);
        }

        private void AssignOwnerName(HtmlDocument doc, Item item)
        {
            string name = GetValueOfField(doc, "Owner Name");
            var careOf = GetValueOfField(doc, "In Care Of");

            if (careOf.ToLower() != "n/a")
            {
                name += " C/O " + careOf;
            }

            AssignNames(item, name);
        }

        private Image GetImage(HtmlDocument doc)
        {
            try
            {
                var node = doc.DocumentNode.SelectSingleNode("//img[contains(@src, 'accountPicture')]");
                if (node == null)
                {
                    node = doc.DocumentNode.SelectSingleNode("//img[contains(@src, 'gisPicture')]");
                }

                var link = node.Attributes["src"].Value;

                link = BaseUrl + "assessor/taxweb/" + link;

                return _webQuery.GetImage(link, 1);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string GetMarketValue(HtmlDocument doc)
        {
            var n = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Full Cash Value (FCV)')]");
            n = n.Sibling("td");

            return WebQuery.Clean(n.InnerText);
        }

        private void AssignOwnerAddress(HtmlDocument doc, Item item)
        {
            var n = GetNodeOfField(doc, "Owner Address");
            string addr1 = WebQuery.Clean(n.InnerText);
            n = n.Sibling("#text");
            string addr2 = WebQuery.Clean(n.InnerText);

            string fullAddress = addr1 + ", " + addr2;

            Utils.AssignFullAddrToOwnerAddress(item, fullAddress, regex: @"(?<city>.+),\s*(?<state>.+) (?<zip>.+)");
        }

        private string GetValueOfField(HtmlDocument doc, string fieldName, bool ommit = true)
        {
            try
            {
                var n = GetNodeOfField(doc, fieldName);
                if (n != null) return WebQuery.Clean(n.InnerText);

                n = GetNodeOfFieldElse(doc, fieldName);
                return WebQuery.Clean(n.InnerText).Trim().Replace(fieldName, "");
            }
            catch
            {
                if (ommit)
                {
                    return "n/a";
                }
                throw;
            }
        }

        private static HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            var n = doc.DocumentNode.SelectSingleNode(string.Format("//b[contains(text(), '{0}')]", fieldName));
            if (n != null) n = n.Sibling("#text");
            return n;
        }

        private static HtmlNode GetNodeOfFieldElse(HtmlDocument doc, string fieldName)
        {
            return doc.DocumentNode.SelectSingleNode(string.Format("//td[contains(*/text(), '{0}')]", fieldName));
        }


        private string GetSearchParameters(HtmlDocument doc, string parcelNumber)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("AllTypes", "ALL");
            dict.Add("docTypeTotal", GetValueOfInput(doc, "docTypeTotal"));
            dict.Add("AccountNumID", "");
            dict.Add("ParcelNumberID", parcelNumber);
            dict.Add("OwnerIDSearchString", "");
            dict.Add("OwnerIDSearchType", "Normal");
            dict.Add("BusinessIDSearchString", "");
            dict.Add("BusinessIDSearchType", "Normal");
            dict.Add("SitusIDhouse", "");
            dict.Add("SitusIDdirectionSuffix", "");
            dict.Add("SitusIDstreet", "");
            dict.Add("SitusIDdesignation", "");
            dict.Add("SitusIDdirection", "");
            dict.Add("SitusIDunit", "");
            dict.Add("CityZipID", "");
            dict.Add("PlattedLegalIDSubdivision", "");
            dict.Add("PlattedLegalIDLot", "");
            dict.Add("PlattedLegalIDBlock", "");
            dict.Add("PlattedLegalIDTract", "");
            dict.Add("PlattedLegalIDUnit", "");
            dict.Add("PlssLegalIDTract", "");
            dict.Add("PlssLegalIDSixteenthSection", "");
            dict.Add("PlssLegalIDQuarterSection", "");
            dict.Add("PlssLegalIDSection", "");
            dict.Add("PlssLegalIDTownship", "");
            dict.Add("PlssLegalIDRange", "");
            dict.Add("accountTypeID", "");
            dict.Add("accountValueIDStart", "");
            dict.Add("accountValueIDEnd", "");
            dict.Add("acresIDStart", "");
            dict.Add("acresIDEnd", "");
            return WebQuery.GetStringFromParameters(dict);
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            throw new NotImplementedException();
        }
    }
}