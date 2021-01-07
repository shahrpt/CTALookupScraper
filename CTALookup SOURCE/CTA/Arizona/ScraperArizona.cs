using CTALookup.Scrapers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTALookup.Arizona
{
    public abstract class ScraperArizona:Scraper
    {
        protected override string FormatParcelNumber(string parcel)
        {
            parcel = parcel.Replace("-", "");
            if (parcel.Length > 9 || parcel.Length < 7)
                throw new Exception("Invalid parcel number. Parcel number should not exceed 9 characters.");
            parcel = string.Format("{0}-{1}-{2}", parcel.Substring(0, 3), parcel.Substring(3, 2), parcel.Substring(5));
            return parcel;
        }
    }
}
