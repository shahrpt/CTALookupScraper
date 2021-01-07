using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public class ScraperSpartanburg : Scraper
    {
        private const string SearchUrl = "http://www.spartanburgcounty.org/asrinfo/search.aspx";
        private const string BaseUrl = "http://www.spartanburgcounty.org/asrinfo/";

        #region Overrides of Scraper

        public override Scraper GetClone()
        {
            return new ScraperSpartanburg();
        }

        public override bool CanScrape(string county)
        {
            return county == "South Carolina:Spartanburg";
        }

        public override Item Scrape(string parcelNumber)
        {
            ResetWebQuery();
            InvokeOpeningSearchPage();
            var doc = _webQuery.GetSource(SearchUrl, 3);

            string parameters = "";
            try
            {
                parameters = GetSearchParameters(doc, parcelNumber);
            }
            catch (Exception ex)
            {
                LogThrowErrorGettingParams(doc, ex);
            }

            InvokeSearching();
            doc = _webQuery.GetPost(SearchUrl, parameters, 1, SearchUrl);

            //If no items found 
            if (doc.DocumentNode.InnerText.Contains("0 Record(s) found"))
            {
                InvokeNoItemsFound();
                return null;
            }

            var linksNodes = doc.DocumentNode.SelectNodes("//table[@id='dgResults']//a");
            if (linksNodes == null)
            {
                LogThrowNotLinkFound(doc);
            }

            var link = WebQuery.BuildUrl(linksNodes[0].Attributes["href"].Value, BaseUrl);

            return GetItem(link);
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            throw new NotImplementedException();
        }

        private string GetValueOfSpanWithId(HtmlDocument doc, string value, bool canBeNull = true)
        {
            try
            {
                return GetInnerText(doc, "span", "id", value);
            }
            catch (Exception)
            {
                if (!canBeNull)
                    throw;
                return "";
            }
        }

        private Item GetItem(string url)
        {
            InvokeOpeningUrl(url);
            var doc = _webQuery.GetSource(url, 1);

            var item = new Item();
            try
            {
                item.MapNumber = GetValueOfSpanWithId(doc, "lblParcelNumberVal");
                item.LegalDescription = GetValueOfSpanWithId(doc, "lblLegalDescriptionVal");
                item.Acreage = GetValueOfSpanWithId(doc, "lblAcreageVal");
                item.PhysicalAddress1 = GetValueOfSpanWithId(doc, "lblPropertyLocationVal");
                string name = GetValueOfSpanWithId(doc, "lblOwnerNameVal");
                AssignNames(item, name);
                if (item.PhysicalAddress1 != "")
                {
                    string[] data = item.PhysicalAddress1.Split(' ');
                    if (data.Length > 0)
                    {
                        int zip = 0;
                        int.TryParse(data[data.Length - 1], out zip);
                        if (zip > 0)
                        {
                            item.PhysicalAddressZip = zip.ToString();
                            item.PhysicalAddressCity = data[data.Length - 2];
                            item.PhysicalAddress1 = item.PhysicalAddress1.Replace(item.PhysicalAddressZip, "").Replace(item.PhysicalAddressCity, "").Trim();
                        }
                        else
                        {
                            item.PhysicalAddressCity = data[data.Length - 1];
                            item.PhysicalAddress1 = item.PhysicalAddress1.Replace(item.PhysicalAddressCity, "").Trim();
                        }

                    }
                }
                item.OwnerAddress = GetValueOfSpanWithId(doc, "lblStreetAddressVal");
                item.OwnerCity = GetValueOfSpanWithId(doc, "lblCityVal");
                item.OwnerState = GetValueOfSpanWithId(doc, "lblStateVal");
                item.OwnerZip = GetValueOfSpanWithId(doc, "lblZipVal");

                item.LandValue = GetValueOfSpanWithId(doc, "lblCurrentAppraisedLandVal");
                item.ImprovementValue = GetValueOfSpanWithId(doc, "lblCurrentAppraisedBuildingVal");
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
                return null;
            }

            item.Images = GetImages(doc);
            return item;
        }

        private string GetSearchParameters(HtmlDocument doc, string parcelNumber)
        {
            string viewState = GetValueOfInput(doc, "__VIEWSTATE");
            var dict = new Dictionary<string, string>
                           {
                               {"__VIEWSTATE", viewState},
                               {"btnSubmit", "Submit"},
                               {"txtName", ""},
                               {"txtDeedVolume", ""},
                               {"txtGisPin", ""},
                               {"txtMapNumber", parcelNumber},
                               {"txtStreet", ""},
                               {"txtDeedPage", ""}
                           };
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}
