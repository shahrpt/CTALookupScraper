using System;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Georgia
{
    public class ScraperChatham : Scraper
    {
        public override bool CanScrape(string county) {
            return county == "Georgia:Chatham";
        }

        public override Scraper GetClone()
        {
            return new ScraperChatham();
        }

        string GetValue(Func<string> func) {
            try {
                return func();
            }
            catch {
                return "n/a";
            }
        }

        public override Item Scrape(string parcelNumber) {
            _webQuery.ClearCookies();

            InvokeSearching();

            var doc =
                _webQuery.GetSource(
                    string.Format("http://boa.chathamcounty.org/Home/PropertyRecordCards.aspx?PIN={0}", parcelNumber), 1);

            var linkNode =
                doc.DocumentNode.SelectSingleNode(
                    "//a[contains(text(), 'Property Record Card') and contains(@id, 'PRCLink')]");
            string url = WebQuery.BuildUrl(WebQuery.Clean(linkNode.Attributes["href"].Value),
                "http://boa.chathamcounty.org");

            InvokeOpeningUrl(url);

            doc = _webQuery.GetSource(url, 1);

            var n = doc.DocumentNode.SelectSingleNode("/html/body/table[2]/tr");

            var item = new Item();
            try {
                AssignNameAndAddress(n, item);
                item.MapNumber = parcelNumber;
                item.LegalDescription = GetValue(() => WebQuery.Clean(n.SelectSingleNode("./td[2]").InnerText));
                item.PhysicalAddress1 = GetValue(() => GetPhysicalAddress(doc));
                item.PhysicalAddressState = "GA";

                AssignMarketLandAndImprovementValues(n, item);

                AssignImage(doc, item);
            }
            catch (Exception ex) {}

            return item;
        }

        private void AssignImage(HtmlDocument doc, Item item) {
            var imgNode = doc.DocumentNode.SelectSingleNode("//a[@class='PicLink']/img");

            if (imgNode == null) {
                Logger.Log("The img node was null");
                Logger.LogCode(doc.DocumentNode.OuterHtml);
                return;
            }

            string src = "http://boa.chathamcounty.org" + imgNode.Attributes["src"].Value;

            if (src.EndsWith("NoPicture.jpg")) {
                return;
            }

            InvokeNotifyEvent("Getting property image");
            item.Image = _webQuery.GetImage(src, 1);
        }

        private void AssignMarketLandAndImprovementValues(HtmlNode node, Item item) {
            item.LandValue = GetValue(() => GetNumericValue(node.SelectSingleNode("./td[4]/table/tr[2]")));
            item.ImprovementValue = GetValue(() => GetNumericValue(node.SelectSingleNode("./td[4]/table/tr[3]")));
            item.MarketValue = GetValue(() => GetNumericValue(node.SelectSingleNode("./td[4]/table/tr[5]")));
        }

        private string GetNumericValue(HtmlNode trNode) {
            return WebQuery.Clean(trNode.SelectSingleNode("./td[2]").InnerText);
        }

        private string GetPhysicalAddress(HtmlDocument doc) {
            var n = doc.DocumentNode.SelectSingleNode("//h2[@style='text-align:right;']");
            return WebQuery.Clean(n.InnerText);
        }

        private void AssignNameAndAddress(HtmlNode node, Item item) {
            var texts = node.SelectNodes("./td[3]/text()");

            if (texts == null || texts.Count < 3 || texts.Count > 4) {
                Logger.Log("Invalid number of rows when parsing Owner Name and Address");
                item.OwnerName = "n/a";
                item.OwnerAddress = "n/a";
                item.OwnerCity = "n/a";
                item.OwnerState = "n/a";
                item.OwnerZip = "n/a";
                return;
            }

            string name = WebQuery.Clean(texts[0].InnerText);

            string cityStateZip = WebQuery.Clean(texts[texts.Count - 1].InnerText);

            string address = WebQuery.Clean(texts[texts.Count - 2].InnerText);

            if (texts.Count == 4) {
                string text = WebQuery.Clean(texts[1].InnerText);

                if (text.ToLower().StartsWith("c/o")){
                    name += " " + text;
                }
                else {
                    address = text + ", " + address;
                }
            }

            AssignNames(item, name);

            item.OwnerAddress = address;
            AssignCityStateZipToOwnerAddress(item, cityStateZip);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }
    }
}
