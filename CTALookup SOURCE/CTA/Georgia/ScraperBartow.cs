using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using CTALookup.Scrapers;

namespace CTALookup.Georgia
{
    public class ScraperBartow : ScraperGeorgia
    {
        public ScraperBartow() : base("Bartow", "ga_bartow",
            "http://qpublic7.qpublic.net/ga_search_dw.php?county=ga_bartow&search=parcel",
            "http://qpublic7.qpublic.net/ga_alsearch_dw.php") {
        }

        public override Scraper GetClone()
        {
            return new ScraperBartow();
        }

        protected override IList<string> GetParcels(HtmlDocument doc) {
            try {
                var nodes = doc.DocumentNode.SelectNodes("//td[@class='search_value'][1]/a");

                return nodes.Select(x => WebQuery.Clean(x.NextSibling.InnerText)).ToList();
            }
            catch (Exception e) {
                return new List<string> { "An error ocurred" };
            }
        }
    }
}
