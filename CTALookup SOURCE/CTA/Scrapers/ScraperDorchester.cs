using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    class ScraperDorchester : Scraper
    {
        #region Overrides of Scraper

        public override bool CanScrape(string county) {
            return county == "South Carolina:Dorchester";
        }

        public override Scraper GetClone()
        {
            return new ScraperDorchester();
        }

        public override Item Scrape(string parcelNumber) {
            ResetWebQuery();
            string parameters = GetParameters(parcelNumber);
            InvokeSearching();
            var doc = _webQuery.GetPost("http://as400.dorchestercounty.net/webapps/ASR001000.pgm", parameters, 1);

            var rows = doc.DocumentNode.SelectNodes("//table[@class='mainlist']//tr");
            if (rows == null) {
                LogMessageCodeAndThrowException("Error getting rows", doc.DocumentNode.OuterHtml);
            }
            if (rows.Count == 1) {
                return null;
            }

            var cells = rows[1].SelectNodes("./td");

            Item item = new Item();
            try
            {
                item.MapNumber = WebQuery.Clean(cells[2].InnerText);
                item.LegalDescription = WebQuery.Clean(cells[8].InnerText);
                item.Acreage = WebQuery.Clean(cells[10].InnerText);
                item.PhysicalAddress1 = WebQuery.Clean(cells[7].InnerText);
                //            item.PhysicalAddressCity =
                //            item.PhysicalAddressState =
                //            item.PhysicalAddressZip =
                string name = WebQuery.Clean(cells[6].InnerText);
                AssignNames(item, name);

                //Opening the details page to obtain more info
                var linkNode = cells[0].SelectSingleNode(".//a[@href]");
                if (linkNode == null)
                {
                    throw new Exception("Error getting details link");
                }
                string link = WebQuery.BuildUrl(linkNode.Attributes["href"].Value,
                                               "http://as400.dorchestercounty.net/webapps/");
                InvokeOpeningUrl(link);
                doc = _webQuery.GetSource(link, 1);

                var mailingAddressNode =
                    doc.DocumentNode.SelectSingleNode("//strong[contains(text(), 'MAILING ADDRESS')]");
                if (mailingAddressNode == null)
                {
                    throw new Exception("Error getting mailing address node");
                }
                var rowNode = mailingAddressNode.ParentNode.ParentNode;
                string address = WebQuery.Clean(rowNode.SelectNodes("./td")[1].InnerText);
                string cityStateZip = WebQuery.Clean(rowNode.NextSibling.NextSibling.SelectNodes("./td")[1].InnerText);
                item.OwnerAddress = address;
                AssignCityStateZipToOwnerAddress(item, cityStateZip);

                var currentTaxableValueNode =
                    doc.DocumentNode.SelectSingleNode("//strong[contains(text(), 'CURRENT TAXABLE VALUE')]");
                if (currentTaxableValueNode == null)
                {
                    throw new Exception("Error getting current taxable node");
                }
                item.MarketValue =
                    WebQuery.Clean(currentTaxableValueNode.ParentNode.ParentNode.SelectNodes("./td")[1].InnerText);
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        public override string GetOwnerName(HtmlDocument doc) {
            throw new NotImplementedException();
        }

        private static string GetParameters(string parcelNumber) {
            string letter = "";
            if (parcelNumber[parcelNumber.Length - 1] == 'C' || parcelNumber[parcelNumber.Length - 1] == 'F') {
                letter = parcelNumber[parcelNumber.Length - 1].ToString();
                parcelNumber = parcelNumber.Substring(0, parcelNumber.Length - 1);
                if (parcelNumber.Length > 10) {
                    parcelNumber = parcelNumber.Substring(0, 10) + "." +
                                   parcelNumber.Substring(10, parcelNumber.Length - 10);
                }

            }
            var dict = new Dictionary<string, string>
                           {
                               {"task", "filter"},
                               {"ww_fPCTMSNO", parcelNumber},
                               {"ww_fPCTMSCF", letter},
                               {"ww_fTNAME", ""},
                               {"ww_fTPROPL", ""},
                               {"ww_fPCMHSERIAL", ""},
                               {"ww_fPCDECAL", ""},
                               {"ww_fPCYEAR", "2011"},
                               {"ww_fBEGINDTE", ""},
                               {"ww_fENDDTE", ""},
                               {"ww_fBEGINACRE", ""},
                               {"ww_fENDACRE", ""}
                           };
            return WebQuery.GetStringFromParameters(dict);
        }

        #endregion
    }
}
