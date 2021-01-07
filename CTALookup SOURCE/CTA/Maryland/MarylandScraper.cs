using System;
using System.Text.RegularExpressions;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Maryland
{
    public abstract class MarylandScraper : Scraper
    {
        protected string CountyId;

        protected MarylandScraper(string countyId) {
            CountyId = countyId;
        }
    }
}
