using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using CTALookup.Scrapers;
using HtmlAgilityPack;

namespace CTALookup.Arizona
{
    class ScraperYavapai : ScraperArizona
    {
        public override bool CanScrape(string county)
        {
            return county.ToLower() == "arizona:yavapai";
        }

        public override Scraper GetClone()
        {
            return new ScraperYavapai();
        }
        private static string format_json(string json)
        {
            dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            return Newtonsoft.Json.JsonConvert.SerializeObject(parsedJson, Newtonsoft.Json.Formatting.Indented);
        }
        public override Item Scrape(string parcelNumber)
        {
            if (parcelNumber.EndsWith("0"))
                parcelNumber = parcelNumber.Substring(0, parcelNumber.Length - 1);
            string parameters = string.Format("{{ \"theVal\": \"{0}\", \"theMode\": \"nothing\"}}", parcelNumber.Replace("-", ""));
            string oldContentType = _webQuery.ContentType;
            _webQuery.ContentType = "application/json; charset=UTF-8";
            //_webQuery.ContentType = "application/x-www-form-urlencoded";
            string url;

              //url = "http://apps.yavapai.us/taxinquiry/YCtaxSearch.asmx/parseText";
            //url = "http://gis.yavapai.us/v4/ycparsearch.asmx/parseText";
            url = "https://gis.yavapai.us/v4/ycparsearch.asmx/parseText";
            
            InvokeSearching();
            var doc = _webQuery.GetPost(url, parameters, 1);
            Console.WriteLine( format_json(doc.DocumentNode.OuterHtml));

            _webQuery.ContentType = oldContentType;

 
            var item = GetItem(doc);
            item.MapNumber = parcelNumber;

            //Get Mailing Address

            _webQuery.ContentType = "application/json; charset=UTF-8";
            parameters = string.Format("{{ \"theVal\": \"{0}\", \"theMode\": \"nothing\"}}", parcelNumber);
            //           doc = _webQuery.GetPost("http://apps.yavapai.us/taxinquiry/YCtaxSearch.asmx/parseText", parameters, 1);
             url = "https://gis.yavapai.us/v4/ycparsearch.asmx/parseText";
           // url = "http://apps.yavapai.us/taxinquiry/YCtaxSearch.asmx/parseText";
            doc = _webQuery.GetPost(url, parameters, 1);
            _webQuery.ContentType = oldContentType;
            Console.WriteLine(format_json(doc.DocumentNode.OuterHtml));
            dynamic d = Newtonsoft.Json.Linq.JObject.Parse(doc.DocumentNode.OuterHtml);
            if (d.d.sumarylist != null && d.d.sumarylist.Count > 0)
            {
                foreach (var summary in d.d.sumarylist)
                {
                    if (summary.Status != null && summary.Status.ToString().Contains("Delinquent Lien"))
                    {
                        item.Description = "PRIOR YEAR LIEN - " + item.Description;
                        item.SetReasonToOmit("PRIOR YEAR LIEN");
                        break;
                    }
                }
            }
            item.LegalDescription = d.d.LegalDesc1;
            string address = d.d.owner.address;
            item.MailingAddressOwner = d.d.owner.name;

            //Parse address
            //Mahmut Duman: Confirmed address coming from Tax Inquiry Page
            AssignMailingAddress(item, address);

            return item;
        }

        private void AssignMailingAddress(Item item, string address)
        {
            if (!string.IsNullOrEmpty(address) && address.TrimStart().StartsWith("c/o", StringComparison.OrdinalIgnoreCase))
            {
                int index = address.IndexOf(item.OwnerAddress);
                if (index >= 0)
                {
                    string co = address.Substring(0, index);
                    item.OwnerAddress += ", " + co.Trim();
                    address = address.Substring(index);
                }
            }
            var m = Regex.Match(address, @"(.+)\s+(.+)\s+(.+)");
            if (!m.Success)
            {
                return;
            }
            item.MailingZip = m.Groups[3].Value;
            item.MailingState = m.Groups[2].Value;

            //Parse the city from the first part

            string lower = m.Groups[1].Value.ToLower();

            bool found = false;
            foreach (var c in UsCities.Cities.Keys)
            {
                string cityLower = c.ToLower();
                if (lower.EndsWith(cityLower))
                {
                    item.MailingCity = cityLower.ToUpper();
                    lower = lower.Replace(cityLower, "").Trim();
                    item.MailingAddress = lower.ToUpper();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                m = Regex.Match(m.Groups[1].Value, @"(.+)\s+(.+)");
                item.MailingAddress = m.Groups[1].Value;
                item.MailingCity = m.Groups[2].Value;
            }
        }

        private Item GetItem(HtmlDocument doc)
        {
            var item = new Item();
            try
            {
                dynamic result = Newtonsoft.Json.Linq.JObject.Parse(doc.DocumentNode.OuterHtml);
                string msg = result.d.msg;

                if (string.IsNullOrEmpty(msg))
                {
 //                   item.Notes += " No Parcel Assessor ";
                    Console.WriteLine("no parcel");
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    if (msg.Contains("This parcel does not exist"))
                    {
                        item.Notes += " No Parcel Assessor";
                        return item;
                    }
                    string parcel = result.d.parcel;
                    return Scrape(parcel);
                }
                else
                {
                    //item.LegalDescription =                     

                    if (result.d.improvements != null && result.d.improvements.Count > 0)
                    {
                        item.PropertyType = result.d.improvements[0].Type;
                        item.Notes += string.Format("SQFT: {0},   YR: {1}", result.d.improvements[0].flrArea, result.d.improvements[0].impYr);
                    }
                    else
                    {
                        item.PropertyType = "No improvements found";
                    }

                    item.Acreage = result.d.dorAcres;
                    item.PhysicalAddress1 = result.d.situsAdd;
                    item.Description = result.d.currLegClass;

                    item.PhysicalAddressState = "AZ";
                    //item.PhysicalAddressCity = result.d.incArea;
                    int ofIndex = item.PhysicalAddressCity.ToLower().IndexOf("of");
                    if (ofIndex > 0)
                    {
                        string[] parts = item.PhysicalAddressCity.Split(new char[] { ' ' });
                        if (parts.Length >= 3 && parts[0].ToLower().Equals("of"))
                            item.PhysicalAddressCity = item.PhysicalAddressCity.Substring(ofIndex + 2).Trim();
                    }
                    string name = result.d.owner.name;
                    string second = result.d.owner.second;
                    if (!string.IsNullOrEmpty(second) && !second.Trim().ToLower().Equals("n/a"))
                    {
                        if (second.ToLower().EndsWith("ttee"))
                        {
                            name += " C/O " + second.Substring(0, second.Length - 4).TrimEnd();
                            item.IsCareOfProcessed = true;
                        }
                        else
                            name += ", " + second;
                    }
                    AssignNames(item, name);

                    item.OwnerAddress = result.d.owner.address;

                    item.OwnerCity = result.d.owner.city;
                    item.OwnerState = result.d.owner.state;
                    item.OwnerZip = result.d.owner.zip;

                    item.MarketValue = result.d.fcvTotal;
                }
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            item.Images = GetImages(doc);
            return item;
        }

        private string GetSplittedParcelInfo(HtmlDocument doc)
        {
            string regex = GetRegex("parlabel", true);
            var m = Regex.Match(doc.DocumentNode.OuterHtml, regex);

            System.Collections.Generic.List<string> parcels = new System.Collections.Generic.List<string>();

            while (m.Success)
            {
                parcels.Add(WebQuery.Clean(m.Groups[1].Value));
                m = m.NextMatch();
            }
            return string.Join(", ", parcels.ToArray());

        }

        private string GetRegex(string fieldName, bool useQuotes)
        {
            string regex = useQuotes ? @"{0}"":""(.+?)""" : @"{0}"":(.+?),";
            return string.Format(regex, fieldName);
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            throw new NotImplementedException();
        }
    }
}
