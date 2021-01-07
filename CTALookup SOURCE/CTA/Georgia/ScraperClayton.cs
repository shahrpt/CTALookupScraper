using CTALookup.Scrapers;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace CTALookup.Georgia
{
    public class ScraperClayton : ScraperGeorgia
    {
        public override bool CanScrape(string county)
        {
            return county == "Georgia:Clayton";
        }

        public ScraperClayton()
            : base("Clayton", "ga_clayton", "")
        { }

        public override Scraper GetClone()
        {
            return new ScraperClayton();
        }

        string GetValue(Func<string> func)
        {
            try
            {
                return func();
            }
            catch
            {
                return "n/a";
            }
        }

        public override Item Scrape(string parcelNumber)
        {
            _webQuery.ClearCookies();

            //InvokeSearching();

            var parts = parcelNumber.Split(' ').ToList();
            parts.RemoveAll(x => String.IsNullOrWhiteSpace(x));
            if (parts.Count != 2) throw new Exception("Wrong format");
            
            var doc =
                _webQuery.GetSource(
                    string.Format("http://weba.co.clayton.ga.us/taxcgi-bin/wtx200r.pgm?parcel={0}", HttpUtility.UrlEncode(parts[0] + "  " + parts[1])), 2);


            var n = doc.DocumentNode.SelectSingleNode("//table/tr/td/table[1]");

            var item = new Item();
            try
            {
                AssignNameAndAddress(n, item);
                item.MapNumber = parcelNumber;
                item.MailingState = "GA"; //some parcel ha another state. is it normal?

                n = doc.DocumentNode.SelectSingleNode("//table[tr/td/text()='LEGAL DESC']");
                if (n != null) AssignLegalDesc(n, item);


                AssignMarketLandAndImprovementValues(doc, item);
                
            }
            catch (Exception ex) { }

            return item;
        }

        private HtmlNode GetTdByText(HtmlDocument doc, String Text, Boolean Sibling = false)
        {
            var res = doc.DocumentNode.SelectSingleNode(String.Format("//table/tr/td[contains(text(), '{0}')]", Text));
            if (res != null && Sibling) res = res.Sibling("td");
            return res;
        }

        private HtmlNode GetTrByTdText(HtmlDocument doc, String Text)
        {
            return doc.DocumentNode.SelectSingleNode(String.Format("//table/tr[contains(td/text(), '{0}')]", Text));
        }

        private HtmlNodeCollection GetTrCellsByTdText(HtmlDocument doc, String Text)
        {
            var res = doc.DocumentNode.SelectSingleNode(String.Format("//table/tr[contains(td/text(), '{0}')]", Text));
            return res.SelectNodes("td/text()");
        }

        private void AssignMarketLandAndImprovementValues(HtmlDocument doc, Item item)
        {
            var node = GetTdByText(doc, "MAP ACRES");
            item.Acreage = GetValueAfterDots(node.InnerText);
            node = GetTrByTdText(doc, "TOTAL PARCEL VALUES").ParentNode;
            var nodes = node.SelectNodes("tr[contains(td/text(), 'FMV')]")[0].SelectNodes("td/text()");
            item.LandValue = WebQuery.Clean(nodes[1].InnerText);
            item.ImprovementValue = WebQuery.Clean(nodes[3].InnerText);
            item.MarketValue = WebQuery.Clean(nodes[5].InnerText);

            node = GetTdByText(doc, "GROUND FLOOR AREA", true);
            var sqr = node == null ? "" : WebQuery.Clean(node.InnerText);
            node = GetTdByText(doc, "ACT/EFF");
            var year = node == null ? "" : GetValueAfterDots(node.InnerText);
            year = year.Split(' ').First();

            node = GetTdByText(doc, "BEDROOMS", true);
            var br = node == null ? "" : WebQuery.Clean(node.InnerText);
            node = GetTdByText(doc, "BATHROOMS", true);
            var ba = node == null ? "" : WebQuery.Clean(node.InnerText);

            AssignNotes(item, sqr, year, br, ba);
        }

        private string GetNumericValue(HtmlNode trNode)
        {
            return WebQuery.Clean(trNode.SelectSingleNode("./td[2]").InnerText);
        }

        String GetValueAfterDots(String Text)
        {
            var res = WebQuery.Clean(Text);
            var parts = res.Split(new[] { " . . " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length < 2 || String.IsNullOrEmpty(parts[1]) ? "n/a" : parts[1];
 
        }

        private String GetTableData(HtmlNode Node, Int32 r, Int32 d)
        {
            var n = Node.SelectSingleNode(String.Format("tr[{0}]/td[{1}]/text()", r, d));
            return n == null ? "" : WebQuery.Clean(n.InnerText);
        }

        private void AssignNameAndAddress(HtmlNode node, Item item)
        {
            var str1 = GetTableData(node, 6, 1);
            var str2 = GetTableData(node, 7, 1);
            var str3 = GetTableData(node, 8, 1);
            var str4 = GetTableData(node.Sibling("table"), 1 ,1);

            if (String.IsNullOrEmpty(str4))
            {
            }
            else
            {
                if (str2.ToLower().StartsWith("c/o") || str1.EndsWith("&"))
                {
                    str1 += " " + str2;
                }
                else
                {
                    item.MailingAddress2 = str2;
                }
                str2 = str3;
                if (str4.Contains(":")) str4 = GetTableData(node.Sibling("table"), 2, 1);
                str3 = str4;
            }
            AssignNames(item, str1);
            item.MailingAddress = str2;
            AssignCityStateZipToMailingAddress(item, str3, "(.+), (.+) (.+)");
            item.PhysicalAddress1 = GetValueAfterDots(GetTableData(node, 7, 2));
        }

        private void AssignLegalDesc(HtmlNode node, Item item)
        {
            var parts = node.SelectNodes("./tr/td/text()").Select(x => WebQuery.Clean(x.InnerText)).ToList();
            parts.RemoveAll(s => String.IsNullOrWhiteSpace(s) || s == "LEGAL DESC");
            item.LegalDescription = String.Join(" ", parts);
        }

        protected override string GetSearchOwnerUrl()
        {
            return "http://weba.co.clayton.ga.us/taxcgi-bin/wtx201r.pgm?btnSrch=Submit+Name+Search&LastName=b";
        }

        protected override IList<string> GetParcels(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//table/tr[1]/td[1]/div/a");
            if (nodes == null)
            {
                return new List<string> { "An error ocurred" };
            }

            return nodes.Select(x => WebQuery.Clean(x.InnerText)).ToList();
        }

        protected override string GetSearchOwnerParameters(string name)
        {
            return "";
        }
    }
}
