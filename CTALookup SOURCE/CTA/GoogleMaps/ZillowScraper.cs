using System;
using System.Text.RegularExpressions;
using System.Web;

namespace CTALookup.GoogleMaps
{
    public class ZillowScraper: IAddressScraper
    {
        private WebQuery _webQuery;

        public ZillowScraper() {
            _webQuery = new WebQuery();
           
        }

        public Address Scrape(string keyword) {
            _webQuery.ClearCookies();

            //https://www.zillow.com/homes/for_sale/1089-BYRMOYCK-TRAIL-ATLANTA,-GA-30319_rb/?fromHomePage=true&shouldFireSellPageImplicitClaimGA=false&fromHomePageTab=buy
            string url =
                $"https://www.zillow.com/search/RealEstateSearch.htm?citystatezip={HttpUtility.UrlEncode(keyword)}";

            var doc = _webQuery.GetSource(url, 1);

            //First, let's see if there is an exact match (full address)
            var nodes =
                   doc.DocumentNode.SelectNodes(
                         $"//header[@class='zsg-content-header addr']//h1[@class='notranslate']");
            if (nodes != null) {
                if (nodes.Count > 1) {
                    return null;
                }
                string addr = WebQuery.Clean(nodes[0].InnerText);
                var addrs = addr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (addrs.Length<3) {
                    Logger.Log(string.Format("An error ocurred getting address from Zillow.com using keyword '{0}'", keyword));
                    Logger.Log(string.Format("The text to be parsed was: '{0}'", addr));
                    return null;
                }

                return new Address {City = addrs[1]};
            }

            //Now, let's see if there are some "Did you mean" suggestions

           nodes =
                doc.DocumentNode.SelectNodes(
                            $"//ul[@class='photo-cards']//li//article//div[@class='zsg-photo-card-caption']//p[@class='zsg-photo-card-spec']//span[@class='zsg-photo-card-address']");
            if (nodes != null) {
                //If multiple matches, ignore
                if (nodes.Count > 1) {
                    return null;
                }

                string addr = WebQuery.Clean(nodes[0].InnerText);

                var addrs = addr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);


                if (addrs.Length < 3) {
                    Logger.Log(string.Format("An error ocurred getting address from Zillow.com using keyword '{0}'", keyword));
                    Logger.Log(string.Format("The text (of type 2) to be parsed was: '{0}'", addr));
                    return null;
                }

                return new Address { City = addrs[1] };

            }

            return null;
        }
    }
}