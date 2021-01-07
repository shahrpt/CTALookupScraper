using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using CTALookup.Scrapers;

namespace CTALookup.Arizona
{
    public class ScraperMaricopa : ScraperApache
    {
        public ScraperMaricopa() {
            TableId = "ui definition stackable table";
        }

        public override bool CanScrape(string county) {
            return county.ToLower() == "arizona:maricopa";
        }

        public override Scraper GetClone()
        {
            return new ScraperMaricopa();
        }

        private string _canadaStatesRegex = @"\s+(BC|AB|SK|MB|YK|ON|QU|NB|NS|PEI)\s+";
        private List<String> _Countries = new List<String>() {"canada","israel","germany","australia"}; 

        private string _sparename;
        private string _spareaddress;
        

        public override Item Scrape(string parcelNumber) {
            InvokeSearching();
            Item item;
            try
            {
                var doc1 = _webQuery.GetSource(string.Format("http://mcassessor.maricopa.gov/?s={0}", parcelNumber), 1);

                var node = doc1.DocumentNode.SelectSingleNode("//div[@class='ui top attached info message']");
                if (node != null && WebQuery.Clean(node.InnerText).StartsWith("we found 0 results", StringComparison.OrdinalIgnoreCase))
                    return null;
                item = GetItem(doc1);
                item.MapNumber = parcelNumber;

                parcelNumber = parcelNumber.Replace("-", "");
            }
            catch (Exception)
            {
                item = new Item();
            }

            //Get Mailing Address info

            string url = string.Format("http://treasurer.maricopa.gov/Parcel/?Parcel={0}", parcelNumber);
            var doc = _webQuery.GetSource(url, 1);

            var spanNode = doc.DocumentNode.SelectSingleNode("//span[contains(@id, 'lblNameAddress')]");
            if (spanNode == null) return item;
            var texts = spanNode.SelectNodes(".//text()").Select(x => WebQuery.Clean(x.InnerText).Trim(',')).Where(x => !string.IsNullOrEmpty(x)).ToList();
            /*if (texts == null || texts.Count == 0 || (texts.Count == 1 && texts[0].ToLower() == "not available"))
            {
                if (!String.IsNullOrEmpty(_sparename)) AssignNames(item, _sparename);
                if (!String.IsNullOrEmpty(_spareaddress)) Utils.AssignFullAddrToPhysicalAddress(item, _spareaddress.Replace("   ", ","));
                return item;
            }*/
            if (texts != null && texts.Count >= 2)
            {
                // Probably C/O with original name and Care of
                if(texts.Count > 3)
                {
                    if (texts[0].ToLower().TrimStart().StartsWith("c/o"))
                    {
                        texts[0] = texts[1] + " " + texts[0];
                        texts.RemoveAt(1);
                    }
                    else if (texts[1].ToLower().TrimStart().StartsWith("c/o"))
                    {
                        texts[0] = texts[0] + " " + texts[1];
                        texts.RemoveAt(1);
                    }
                }

                if (texts[0].ToLower().StartsWith("c/o") && texts.Count > 3)
                {
                    texts[0] = texts[1] + " " + texts[0];
                    texts.RemoveAt(1);
                }

                var country = _Countries.Find(x => x == texts.Last().ToLower());
                if (!string.IsNullOrEmpty(country))
                {
                    country = texts.Last();
                    texts.RemoveAt(texts.Count - 1);
                }

                if (texts.Count > 3 && !Char.IsNumber(texts[1].First()))
                {
                    /*if (Utils.TextContainsCompanyWords(texts[0]))
                    {
                        item.Company = texts[0];
                        texts.RemoveAt(0);
                    }
                    else
                    {
                        texts[0] += " " + texts[1];
                        texts.RemoveAt(1);
                    }*/
                    AssignNames(item, texts[0], texts[1]);
                    texts.RemoveAt(0);
                }
                else
                    //if (String.IsNullOrEmpty(item.OwnerAddress))
                    AssignNames(item, texts[0]);

                string addr = texts[1];
                string cityStateZip = texts[2];

                item.MailingAddress = addr;

                //If external country
                //if (texts.Count == 4) {
                if (!String.IsNullOrEmpty(country))
                {
                    switch (country.ToLower())
                    {
                        case "canada":
                            {
                                var m = Regex.Match(cityStateZip, @"(.+)" + _canadaStatesRegex + @"(.+)");
                                if (!m.Success)
                                {
                                    Logger.Log(string.Format("Error parsing city, state and zip from Canada address: {0}",
                                        cityStateZip));
                                    item.MailingCity = "<error>";
                                    item.MailingState = "<error>";
                                    item.MailingZip = "<error>";
                                }
                                else
                                {
                                    item.MailingCity = m.Groups[1].Value;
                                    item.MailingState = m.Groups[2].Value;
                                    item.MailingZip = m.Groups[3].Value;
                                }
                                break;
                            }
                        case "israel":
                        case "australia":
                            {
                                var m = cityStateZip.Split(' ');
                                item.MailingCity = String.Join(" ", m.Take(m.Count() - 1));
                                item.MailingState = country;
                                item.MailingZip = m.Last();
                                break;
                            }
                        case "germany":
                            {
                                var m = cityStateZip.Split(' ');
                                item.MailingCity = String.Join(" ", m.Skip(1));
                                item.MailingState = country;
                                item.MailingZip = m.First();
                                break;
                            }

                    }
                }
                else
                {
                    try
                    {
                        AssignCityStateZipToMailingAddress(item, cityStateZip, @"(.+),\s*(.+)\s+(.+)");
                    }
                    catch { item.MailingCity = cityStateZip; }
                }
            }

            if (String.IsNullOrEmpty(item.OwnerFirstName) && String.IsNullOrEmpty(item.Company) && !String.IsNullOrEmpty(_sparename))
                AssignNames(item, _sparename);

            spanNode = doc.DocumentNode.SelectSingleNode("//span[contains(@id, 'lblSitusAddress')]");
            if (spanNode != null)
            {
                texts = spanNode.SelectNodes(".//text()").Select(x => WebQuery.Clean(x.InnerText).Trim(',')).Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (texts.Count >= 2)
                {
                    item.PhysicalAddress1 = String.Join(",", texts.Take(texts.Count - 1));
                    AssignCityStateZipToPhysicalAddress(item, texts[texts.Count - 1], regex: @"(?<city>.+),\s*(?<state>.+) (?<zip>.+)");
                }
            }
            else if (!String.IsNullOrEmpty(_spareaddress)) Utils.AssignFullAddrToPhysicalAddress(item, _spareaddress.Replace("   ", ","));

            spanNode = doc.DocumentNode.SelectSingleNode("//span[contains(@id, 'lblLegalDescription')]");
            if (spanNode != null)
            {
                texts = spanNode.SelectNodes(".//text()").Select(x => WebQuery.Clean(x.InnerText).Trim(',')).Where(x => !string.IsNullOrEmpty(x)).ToList();
                item.LegalDescription = String.Join(" ", texts);
            }

            doc = _webQuery.GetSource("http://treasurer.maricopa.gov/Parcel/Summary.aspx?List=All", 1, "http://treasurer.maricopa.gov/Parcel/Summary.aspx");
            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@id, 'divBackTaxes')]//td[contains(text(), 'Tax Lien')]");

            if (nodes != null && nodes.Count > 0)
            {
                if (nodes.Any(x => x.InnerHtml.ToLower().Contains("pay with certified funds") &&
                    !(x.InnerHtml.ToLower().Contains("available tax lien"))))
                {
                    item.Description = "PRIOR YEAR DELINQUENCY    " + item.Description;
                    item.SetReasonToOmit("PRIOR YEAR DELINQUENCY");
                }
                else if (nodes.Any(x => x.InnerHtml.ToLower().Contains("available tax lien"))) item.Description = "Available Tax Lien    " + item.Description;
            }
            GetAdditionalOwner(item);
            return item;
        }

        private String GetResultSearch(params String[] Params)
        {
            var doc = _webQuery.GetSource(string.Format("http://mcassessor.maricopa.gov/?s={0}", String.Join(" ", Params)), 1);
            var nodes = doc.DocumentNode.SelectNodes("//div[@id='rp-results']/div");
            if (nodes == null || nodes.Count != 2)
            {
                return null;
            }
            var firstskip = true;
            foreach (var node in nodes)
            {
                if (firstskip)
                {
                    firstskip = false;
                    continue;
                }
                var str = WebQuery.Clean(node.SelectSingleNode("//div[contains(text(), ' - ')]").InnerText);
                str = str.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                return str;
            }
            return null;
        }
        private void GetAdditionalOwner(Item item)
        {
            if (String.IsNullOrEmpty(item.MailingAddress)) return;
            try
            {
                var parts = Utils.SplitAddress(item.MailingAddress);
                var str = GetResultSearch(parts[0], item.MailingCity);
                if (str == null) str = GetResultSearch(parts[0], parts[1].Replace("STE ", ""), item.MailingCity);
                if (str == null) str = GetResultSearch(parts[0], item.MailingCity, item.MailingZip.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
                if (str == null) str = GetResultSearch(parts[0], parts[1].Replace("STE ", ""), item.MailingCity, item.MailingZip.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault());
                if (str == null) str = "n/a";
                item.MailingAddressOwner = str;
            }
            catch { item.MailingAddressOwner = "n/a"; }

        }

        protected override HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            var node =
               doc.DocumentNode.SelectSingleNode(
                   string.Format("//table[@class='{0}']//tr/td[1][contains(text(), '{1}')]", TableId, fieldName));

            var next = node.Sibling("td");
            return next;
        }

        protected override Item GetItem(HtmlDocument doc) {
            var item = new Item();
            try {
                if (doc.DocumentNode.InnerHtml.Contains("We couldn't find an exact match")) return item;
                item.LegalDescription = GetValueOfField(doc, "Description");

                _spareaddress = GetValueOfField(doc, "Address");
                //Utils.AssignFullAddrToPhysicalAddress(item, text.Replace("   ", ","));

                string text = GetValueOfField(doc, "Mailing Address").Replace("\n", "").Replace("USA", "").Trim();
                Utils.AssignFullAddrToOwnerAddress(item, text, regex: @"(?<city>.+),\s*(?<state>.+) (?<zip>.+)");

                _sparename = GetValueOfField(doc, "Owner").TrimEnd(',', ' ');
                //AssignNames(item, name);

                item.MarketValue = GetValue(doc, "Full Cash Value");
                item.LandValue = GetValue(doc, "Assessed LPV");

                var node = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'Legal Class') or contains(a/text(), 'Legal Class')]");
                if (node != null)
                {
                    node = node.ParentNode.Sibling("tr");
                    node = node.SelectSingleNode("td").Sibling("td");
                    item.PropertyType = WebQuery.Clean(node.InnerText);
                }

                item.Description += "D#: " + GetValue(doc, "Deed") + " DD: " + GetValue(doc, "Deed Date") +
                    // This code line was added to include Sales Date and Saled Price to Description Field
                    " SD: " + GetValue(doc, "Sale Date") + " SP: " + GetValue(doc, "Sale Price");
                var year = GetValue(doc, "Construction Year");
                var ft = GetValue(doc, "Living Area");
                AssignNotes(item, ft, year, "", "");
                AssignAdditionalNote(doc, item, "Pool");

                //var res = GetValue(doc, "PU Description");
                var res = GetValue(doc, "Description");
                if (!String.IsNullOrEmpty(res) && res != "n/a")
                    item.Notes = res + " " + item.Notes;
                //AssignAdditionalNote(doc, item, "Description", "Descr");
            }
            catch (Exception ex) {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private void AssignAdditionalNote(HtmlDocument doc, Item item, String TableCaption, String Abbr = null)
        {
            var res = GetValue(doc, TableCaption);
            if (!String.IsNullOrEmpty(res) && res != "n/a")
                item.Notes += String.Format("    {0}: {1}", String.IsNullOrEmpty(Abbr) ? TableCaption : Abbr, res);
        }

        private string GetValue(HtmlDocument doc, String Caption) {
            var node = doc.DocumentNode.SelectSingleNode(String.Format("//td[contains(text(), '{0}') or contains(a/text(), '{0}')]", Caption));
            if (node == null) return "";
            //var tdNode = node.Ancestors("td").First();

            var tdNode = node.Sibling("td");//tdNode.Sibling("td");

            return WebQuery.Clean(tdNode.InnerText);
        }

        public override string GetOwnerName(HtmlDocument doc) {
            return "n/a";
        }
    }
}
