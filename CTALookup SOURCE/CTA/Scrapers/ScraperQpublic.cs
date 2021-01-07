using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public abstract class ScraperQpublic : Scraper
    {
        protected string BaseUrl = "http://qpublic5.qpublic.net/";
        public string CountyCode;
        protected string SearchUrl;
        protected string SubmitUrl;
        protected string XpathLinkNodes;

        protected virtual string GetParameters(string parcelNumber, bool addCountyCode = true)
        {
            var dict = new Dictionary<string, string>
            {
                {"BEGIN", "0"},
                {"INPUT", parcelNumber},
                {"searchType", "parcel_id"},
                {"Parcel_Search", "Search By Parcel ID"}
            };
            if (addCountyCode)
            {
                dict.Add("county", CountyCode);
            }
            return WebQuery.GetStringFromParameters(dict);
        }

        protected static bool NoRecordsFound(HtmlDocument doc)
        {
            return doc.DocumentNode.OuterHtml.Contains("No Records Found");
        }
         
        protected HtmlDocument SubmitQuery(string parameters)
        {
            //return _webQuery.GetSource(SubmitUrl + "?" + parameters, 1, SearchUrl);
           return _webQuery.GetPost(SubmitUrl, parameters, 1, SearchUrl);
           
        }

        protected HtmlDocument Search(string parcelNumber, bool addCountyCode = true)
        {
            ResetWebQuery();
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource(SearchUrl, 1);

            var parameters = GetParameters(parcelNumber, addCountyCode);

            InvokeSearching();
            doc = SubmitQuery(parameters);
            return doc;
        }

        protected virtual string GetLink(HtmlDocument doc)
        {
//            var linkNodes = doc.DocumentNode.SelectNodes(XpathLinkNodes);
//            if (linkNodes == null)
//            {
//                LogThrowNotLinkFound(doc);
//            }
//            string link = linkNodes[0].SelectSingleNode(".//a[@href]").Attributes["href"].Value;
//            link = WebQuery.BuildUrl(link, BaseUrl);
//            return link;

            var node = doc.DocumentNode.SelectSingleNode("//td[@class='search_value'][1]/a");
            if (node == null)
            {
                LogThrowNotLinkFound(doc);
            }
            var link = WebQuery.BuildUrl(node.Attributes["href"].Value, BaseUrl);

            return link;
        }

        protected static string GetValueOfField(HtmlDocument doc, string fieldName, bool ommitIfNotExists = true)
        {
            var node = GetNodeOfField(doc, fieldName);
            if (node == null)
            {
                if (ommitIfNotExists)
                {
                    return "n/a";
                }
                throw new Exception(string.Format("Error getting value of field {0}", fieldName));
            }

            return WebQuery.Clean(node.InnerText).Replace("\n", "");
        }

        protected static HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            return
            (from n in
                doc.DocumentNode.SelectNodes(
                    "//td[@class='owner_header' or @class='owner_hearer' or @class='cell_header']")
                where n.InnerText.Contains(fieldName)
                select n.NextSibling.Name == "#text" ? n.NextSibling.NextSibling : n.NextSibling).FirstOrDefault();
        }

        protected static string CleanValue(string text)
        {
            try
            {
                return Regex.Replace(text, @"(.*?)(\s+)(.*)",
                    m => m.Groups[1].Value + " " + m.Groups[3].Value);
            }
            catch
            {
                return text;
            }
        }

        protected string GetValueFromPreliminaryValueTable(HtmlDocument doc, int columnIndex,
            bool ommitIfNotExists = true)
        {
            try
            {
                var taxValuesNodes = doc.DocumentNode.SelectNodes("//td[@class='tax_value']");
                //The Land Value is the first one:
                if (taxValuesNodes == null)
                {
                    LogMessageCodeAndThrowException("Error getting land value", doc.DocumentNode.OuterHtml);
                }
                if (columnIndex >= taxValuesNodes.Count)
                {
                    LogMessageCodeAndThrowException("Error getting land value. Field is greather",
                        doc.DocumentNode.OuterHtml);
                }
                var result = WebQuery.Clean(taxValuesNodes[columnIndex].InnerText);
                return CleanValue(result);
            }
            catch
            {
                if (ommitIfNotExists)
                {
                    return "n/a";
                }
                throw;
            }
        }

        protected void AssignOwnerAddress(Item item, HtmlDocument doc, string regex = "(.+), (.+) (.+)")
        {
            var texts = GetOwnerAddress(doc);

            if (texts[0].ToLower().StartsWith("c/o"))
            {
                //It belongs to the name
                var name = item.OwnerName + " " + texts[0];
                AssignNames(item, name);

                item.OwnerAddress = texts[1];
                SplitLinealOwnerAddress(item);
            }
            else
            {
                item.OwnerAddress = texts[0];
                //item.OwnerAddress2 = "";
                //item.OwnerAddress = $"{texts[0]} {texts[1]}";
                AssignCityStateZipToOwnerAddress(item, texts[1], regex);
            }
        }


        protected static string[] GetOwnerAddress(HtmlDocument doc)
        {
            var address = GetValueOfField(doc, "Mailing Address");

            var node = GetNodeOfField(doc, "Mailing Address");
            var trNode = node.ParentNode;
            var tr2 = trNode.NextSibling.NextSibling;
            var tdNodes = tr2.SelectNodes("./td[@class='owner_value' or @class='cell_value']");

            var secondLine = WebQuery.Clean(tdNodes[0].InnerText);

            return new[] {address, secondLine};
        }

        public virtual string GetVerticalValueFromQpublic(HtmlDocument doc, params string[] fieldNames)
        {
            HtmlNode tdNode = null;
            doc.OptionAutoCloseOnEnd = true;

            IList<string> fields = fieldNames.ToList();
            foreach (var f in fieldNames)
            {
                fields.Insert(0, f.Replace(" ", ""));
            }

            foreach (var fieldName in fields)
            {
                tdNode =
                    doc.DocumentNode.SelectNodes("//td")
                        .FirstOrDefault(
                            x =>
                                x.SelectSingleNode(".//td") == null &&
                                WebQuery.Clean(x.InnerText).ToLower().Contains(fieldName.ToLower()));
                if (tdNode != null)
                {
                    break;
                }
            }

            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            if (trNode == null)
            {
                return "n/a";
            }

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);

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
                    if (trNode == null)
                        return "n/a";
                }
            }

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);

            return Regex.Replace(value, @"\s+", " ");
        }

        public virtual string GetVerticalValueFromQpublicEx(HtmlDocument doc, params string[] fieldNames)
        {
            HtmlNode tdNode = null;
            doc.OptionAutoCloseOnEnd = true;

            var nodes = doc.DocumentNode.SelectNodes("//td");
            foreach (var node in nodes)
            {
                var textNodes = node.SelectNodes("text() | */text()");
                if (textNodes == null || textNodes.Count == 0)
                {
                    continue;
                }
                var texts = textNodes.Select(x => WebQuery.Clean(x.InnerText?.Replace("<br/>","")));
                var text = string.Join(" ", texts);
                foreach (var fieldName in fieldNames)
                {
                    if (!text.ToLower().Contains(fieldName.ToLower()))
                    {
                        continue;
                    }
                    
                    if (node.Ancestors("tr").FirstOrDefault() == null ||
                        node.Ancestors("tr").First().Ancestors("table").FirstOrDefault() == null)
                    {
                        continue;
                    }
                    var table = node.Ancestors("tr").First().Ancestors("table").First();
                    if (table.SelectNodes("tr").IndexOf(node.Ancestors("tr").First()) == 2)
                    {
                        tdNode = node;
                        break;
                    }
                }
                if (tdNode != null)
                {
                    break;
                }
            }
            //      .FirstOrDefault(
            //          x => x.SelectSingleNode(".//td") == null && WebQuery.Clean(x.InnerText).ToLower().Contains(fieldName.ToLower()));
            /*
            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);


            if (trNode.NextSibling == null)
            {
                trNode = trNode.SelectSingleNode("./tr");
            }
            else
            {
                trNode = trNode.NextSibling;
                while (trNode.Name.ToLower() != "tr")
                {
                    var tmp = trNode.NextSibling;
                    if (tmp == null)
                    {
                        break;
                    }
                    trNode = tmp;
                }
            }

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes == null || Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);
            +
            return Regex.Replace(value, @"\s+", " ");*/
            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);

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

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);

            return Regex.Replace(value, @"\s+", " ");
        }
    }
}