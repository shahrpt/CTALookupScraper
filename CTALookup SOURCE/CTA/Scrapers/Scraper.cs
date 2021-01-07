using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace CTALookup.Scrapers
{
    public abstract class Scraper
    {
        public delegate void NotifyEventHandler(string msg);

        protected const string ErrorGettingResultLinks = "Error getting result links";

        private static string[] _companyKeywords = { "revocable", "living" };
        private static string[] _nameKeywords = { "trustee" };

        protected WebQuery _webQuery;

        protected Scraper()
        {
            Delay = 5;
            ResetWebQuery();
        }

        public int Delay { get; set; }

        public bool UseProxy { get; set; }

        public event NotifyEventHandler NotifyEvent;

        protected void ResetWebQuery()
        {
            _webQuery = new WebQuery
            {
                Delay = Delay
            };
        }

        public abstract bool CanScrape(string county);
        public abstract Item Scrape(string parcelNumber);
        public abstract string GetOwnerName(HtmlDocument doc);
        public abstract Scraper GetClone();

        public void InvokeNotifyEvent(string msg)
        {
            var handler = NotifyEvent;
            if (handler != null) handler(msg);
        }

        protected static void LogMessageCodeAndThrowException(string msg, string code = null, Exception ex = null)
        {
            Logger.Log(msg);
            if (ex != null) Logger.LogException(ex);
            if (code != null) Logger.LogCode(code);
            //throw new Exception(msg);
        }

        protected void SplitLinealOwnerAddress(Item item, bool cityToUpper = false)
        {
            var data = item.OwnerAddress.Split(',').ToList();

            if (data.Count > 2)
            {
                var str1 = "";
                for (var i = 0; i < data.Count - 1; i++) str1 += data[i] + ", ";
                str1 = str1.TrimEnd(' ', ',');
                data = new List<string> { str1, data[data.Count - 1] };
            }

            if (data.Count > 0)
            {
                string[] temp;
                var lower = data[0].ToLower();
                var found = false;
                foreach (var c in UsCities.Cities.Keys)
                    if (lower.EndsWith(c.ToLower()))
                    {
                        item.OwnerCity = c;
                        if (cityToUpper) item.OwnerCity = item.OwnerCity.ToUpper();
                        data[0] = Regex.Replace(data[0], c, "", RegexOptions.IgnoreCase);
                        found = true;
                        break;
                    }
                if (!found)
                {
                    temp = data[0].Trim().Split(' ');
                    if (temp.Length > 0) item.OwnerCity = temp[temp.Length - 1];
                }
                item.OwnerAddress = data[0].Replace(item.OwnerCity, "").Trim();
                temp = data[data.Count - 1].Trim().Split(' ');
                if (temp.Length > 0) item.OwnerState = temp[0].Trim();
                if (temp.Length > 1) item.OwnerZip = temp[1].Trim();
            }
        }

        protected string GetInnerText(HtmlDocument doc, string tag, string attribute, string value)
        {
            var node = GetNode(doc, tag, attribute, value);
            if (node == null)
                throw new Exception(string.Format("Error getting InnerText: Tag: {0}, Attribute: {1}, Value: {2}",
                    tag,
                    attribute,
                    value
                ));
            return WebQuery.Clean(node.InnerText);
        }

        protected string GetValueOfAttribute(HtmlDocument doc, string tag, string attribute, string value, string name)
        {
            var node = GetNode(doc, tag, attribute, value);
            if (node == null)
                throw new Exception(
                    string.Format("Error getting attribute value: Tag: {0}, Attribute: {1}, Value: {2}, Name: {3}",
                        tag,
                        attribute,
                        value,
                        name));

            return WebQuery.Clean(node.GetAttributeValue(name, ""));
        }

        protected static HtmlNode GetNode(HtmlDocument doc, string tag, string attribute, string value)
        {
            return doc.DocumentNode.SelectSingleNode(string.Format("//{0}[@{1}='{2}']", tag, attribute, value));
        }

        protected void LogThrowNotLinkFound(HtmlDocument doc)
        {
            LogMessageCodeAndThrowException(ErrorGettingResultLinks, doc.DocumentNode.OuterHtml);
        }

        protected void InvokeNoItemsFound()
        {
            InvokeNotifyEvent("No items found");
        }

        protected void InvokeSearching()
        {
            InvokeNotifyEvent("Searching");
        }

        protected void InvokeOpeningSearchPage()
        {
            InvokeNotifyEvent("Opening search page");
        }

        protected void InvokeOpeningUrl(string url)
        {
            InvokeNotifyEvent(string.Format("Opening url {0}", url));
        }
        //private static string[] _allKeywords = _companyKeywords.Union(_nameKeywords).ToArray();

        public void SetAddressNotes(Item item)
        {
            var addressPrefix = "";
            var len1 = item.OwnerAddress?.Length ?? 0;
            var len2 = item.PhysicalAddress1?.Length ?? 0;

            var minLen = Math.Min(len1, len2);
            minLen = Math.Min(minLen, 5);


            if (minLen >= 3 && string.Compare(item.OwnerAddress?.Substring(0, minLen - 1),
                    item.PhysicalAddress1?.Substring(0, minLen - 1), StringComparison.InvariantCultureIgnoreCase) == 0 &&
                string.Compare(item.OwnerCity, item.PhysicalAddressCity,
                    StringComparison.InvariantCultureIgnoreCase) == 0 &&
                (string.Compare(item.OwnerState, item.PhysicalAddressState,
                     StringComparison.InvariantCultureIgnoreCase) == 0 || string.Compare(item.OwnerState, item.State,
                     StringComparison.InvariantCultureIgnoreCase) == 0))
                addressPrefix = "Owner Resident.";
            else if (item.OwnerState == item.PhysicalAddressState || item.OwnerState == item.State)
                addressPrefix = "Non-Resident Owner.";
            else
                addressPrefix = "Out of State Owner.";


            if (!string.IsNullOrWhiteSpace(addressPrefix))
                item.Notes = $"{addressPrefix} {item.Notes}";
        }

        public void PostProcess(Item item)
        {
            //var m = Regex.Match(item.OwnerAddress, @"(^\s*C/O.+?)(?:\d| PO )", RegexOptions.IgnoreCase);
            //if (m.Success)
            //{
            //    string name = m.Groups[1].Value.Trim();
            //    item.OwnerAddress = item.OwnerAddress.Replace(name, "").Trim(' ', ',');

            //    item.OwnerName += " " + name;

            //    AssignNames(item, item.OwnerName);
            //}

            //Handle C/O
            var coIndex = item.OwnerAddress.IndexOf("c/o", StringComparison.OrdinalIgnoreCase);
            if (coIndex > 0)
            {
                var name = item.OwnerAddress.Substring(coIndex);
                item.OwnerAddress = item.OwnerAddress.Replace(name, "").Trim(' ', ',');

                item.OwnerName += " " + name;
                if (!item.IsCareOfProcessed)
                    AssignNames(item, item.OwnerName);
            }

            //Handle Living Trust
            if (item.Interested == "Y" && item.OwnerName.IndexOf("living tr", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.Interested = "N";
                item.ReasonToOmit = "LIVING TRUST";
            }


            //Remove Physical Address extra i.e Town of city of
            if (!string.IsNullOrEmpty(item.PhysicalAddressCity))
            {
                if (item.PhysicalAddressCity.StartsWith("town of", StringComparison.OrdinalIgnoreCase)
                    || item.PhysicalAddressCity.StartsWith("city of", StringComparison.OrdinalIgnoreCase))
                    item.PhysicalAddressCity = item.PhysicalAddressCity.Substring(7).TrimStart();
                if (item.PhysicalAddressCity.EndsWith(" FD"))
                    item.PhysicalAddressCity = item.PhysicalAddressCity
                        .Substring(0, item.PhysicalAddressCity.Length - 3)
                        .Trim();
            }

            //See if we need to handle TRUSTEE, LIVING and REVOCABLE
            /*else {
                //If any the keyword is on the Owner Name field
                if (_allKeywords.Any(x => item.OwnerName.ToLower().Contains(x))) {
                    bool company = _companyKeywords.Any(x => item.OwnerName.ToLower().Contains(x));
                    foreach (var k in _allKeywords) {
                        item.OwnerName = Regex.Replace(item.OwnerName, string.Format("{0}.*", k),
                            "", RegexOptions.IgnoreCase);
                    }

                    item.OwnerName = item.OwnerName.Trim();

                    if (company) {
                        item.ClearNameFields();
                        item.Company = item.OwnerName;
                    }
                    else {
                        item.Company = "";
                        AssignNames(item, item.OwnerName, firstMiddleLast: true);
                    }
                }
            }*/

            //Split Owner Address into Address1 and Address2
            if (!string.IsNullOrEmpty(item.OwnerAddress))
            {
                var addresses = Utils.SplitAddress(item.OwnerAddress);
                item.OwnerAddress = addresses[0];
                item.OwnerAddress2 = addresses[1];
            }

            //Split Mailing Address into into Address1 and Address2
            if (!string.IsNullOrEmpty(item.MailingAddress))
            {
                var addresses = Utils.SplitAddress(item.MailingAddress);
                item.MailingAddress = addresses[0];
                item.MailingAddress2 = addresses[1];
            }
        }

        protected void AssignCityStateZipToPhysicalAddress(Item item, string text, string regex = null)
        {
            var city = "";
            var state = "";
            var zip = "";
            Utils.GetCityStateZip(text, ref city, ref state, ref zip, regex);
            item.PhysicalAddressCity = city;
            item.PhysicalAddressState = state;
            item.PhysicalAddressZip = zip;
        }

        protected void AssignCityStateZipToOwnerAddress(Item item, string text, string regex = null)
        {
            var city = "";
            var state = "";
            var zip = "";
            Utils.GetCityStateZip(text, ref city, ref state, ref zip, regex);
            item.OwnerCity = Utils.CleanMultipleSpaces(city);
            item.OwnerState = Utils.CleanMultipleSpaces(state);
            item.OwnerZip = Utils.CleanMultipleSpaces(zip);
        }

        protected void AssignCityStateZipToMailingAddress(Item item, string text, string regex = null)
        {
            var city = "";
            var state = "";
            var zip = "";
            Utils.GetCityStateZip(text, ref city, ref state, ref zip, regex);
            item.MailingCity = city;
            item.MailingState = state;
            item.MailingZip = zip;
        }

        protected void AssignAddressCityStateZipToOwnerAddress(Item item, string text)
        {
            var city = "";
            var state = "";
            var zip = "";
            var address = "";
            Utils.GetAddress(text, ref address, ref city, ref state, ref zip);
            item.OwnerAddress = address;
            item.OwnerCity = city;
            item.OwnerState = state;
            item.OwnerZip = zip;
        }

        protected void AssignNames(Item item, string text, string text2)
        {
            var fake = new Item();
            string otherText = null;
            if (Utils.TextContainsCompanyWords(text))
            {
                AssignNames(fake, text);
                otherText = text2;
            }
            else if (Utils.TextContainsCompanyWords(text2))
            {
                AssignNames(fake, text2);
                otherText = text;
            }
            if (Utils.CheckAvoidWords(ref text, false)) otherText = text;
            else if (Utils.CheckAvoidWords(ref text2, false)) otherText = text2;

            if (otherText == null)
            {
                AssignNames(item, text + " " + text2);
            }
            else
            {
                AssignNames(item, otherText);
                item.Company = fake.Company;
                item.OwnerName = text + ", " + text2;
            }
        }

        protected void AssignNames(Item item, string text, bool firstMiddleLast = false)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var oText = text;
            var existWord = Utils.CheckAvoidWords(ref oText);

            var estateStr = "estate of ";
            var ind = text.IndexOf(estateStr, StringComparison.CurrentCultureIgnoreCase);
            if (ind > -1)
            {
                item.Company = text.Substring(ind);
                text = text.Substring(ind + estateStr.Length);
            }
            if (text[0] == '&') text = text.Remove(0, 1);
            var fullName = "";
            var firstName = "";
            var middleInitial = "";
            var lastName = "";


            Utils.GetNames(existWord ? oText : text, ref fullName, ref firstName, ref middleInitial, ref lastName,
                firstMiddleLast);
            if ((firstName.Length < 2 || lastName.Length < 2) && !(firstName.Length == 0 && lastName.Length > 2))
            {
                Utils.GetNames(existWord ? oText : text, ref fullName, ref firstName, ref middleInitial, ref lastName,
                    true);
                if (firstName.Length < 2 || lastName.Length < 2)
                    Utils.GetNames(existWord ? oText : text, ref fullName, ref firstName, ref middleInitial,
                        ref lastName, firstMiddleLast);
            }

            if (existWord)
                fullName = text;
            item.OwnerName = fullName;


            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(middleInitial))
            {
                item.Company = lastName.Split('/')[0];
            }
            else
            {
                item.OwnerFirstName = firstName;
                item.OwnerMiddleInitial = middleInitial;
                item.OwnerLastName = lastName;
            }
        }

        protected void AssignNotes(Item item, string square, string year, string bedrooms, string bathrooms)
        {
            var str = "";
            if (!string.IsNullOrEmpty(square) && square != "n/a")
                str = "sqft: " + square;

            var tmpstr = string.IsNullOrEmpty(bedrooms) || bedrooms == "n/a" ? "" : bedrooms;
            if (!string.IsNullOrEmpty(bathrooms) && bathrooms != "n/a")
                tmpstr += (string.IsNullOrEmpty(tmpstr) ? "n/a/" : "") + bathrooms;
            if (!string.IsNullOrEmpty(tmpstr))
                str += (string.IsNullOrEmpty(str) ? "" : "    ") + "BR/BA: " + tmpstr;

            if (!string.IsNullOrEmpty(year) && year != "n/a")
                str += (string.IsNullOrEmpty(str) ? "" : "    ") + "YR: " + year;
            item.Notes = str;
        }

        protected static void LogThrowErrorInField(HtmlDocument doc, Exception ex)
        {
            LogMessageCodeAndThrowException(
                "An error ocurred getting some field from the page. Check log for more details",
                doc.DocumentNode.OuterHtml, ex);
        }

        protected void LogThrowErrorGettingParams(HtmlDocument doc, Exception ex)
        {
            LogMessageCodeAndThrowException("Error getting search parameters. Check log for more details",
                doc.DocumentNode.OuterHtml, ex);
        }

        protected string GetValueOfInput(HtmlDocument doc, string inputId)
        {
            return GetValueOfAttribute(doc, "input", "name", inputId, "value");
        }

        protected string GetValueOfComboBox(HtmlDocument doc, string comboId)
        {
            var node = GetNode(doc, "select", "name", comboId);
            if (node == null)
                throw new Exception("Error getting attribute value: Tag: select, Attribute: name, Value: " + comboId);
            node = node.SelectSingleNode("option[@selected]");
            if (node == null) return "";
            return node.GetAttributeValue("value", "");
        }

        protected virtual string FormatParcelNumber(string parcel)
        {
            parcel = Regex.Replace(parcel, "(.+)-([A-Z])$", "$1$2");
            return parcel;
        }

        public string PreProcess(string parcel)
        {
            return FormatParcelNumber(parcel);
        }

        public void AddAdditionalNote(Item item, string Caption, string Value, string Suffix = "")
        {
            if (!string.IsNullOrEmpty(Value) && Value != "n/a")
                item.Notes += string.Format("{0}{1}: {2}{3}", string.IsNullOrEmpty(item.Notes) ? "" : "    ", Caption,
                    Value, Suffix);
        }

        public List<ImageInfo> GetImages(HtmlDocument htmlDocument)
        {
            List<ImageInfo> images = new List<ImageInfo>();
            try
            {
                var imageNodes = htmlDocument.DocumentNode.SelectNodes("//div[@id='photogrid']//div[@class='photo-thumbnail']//img");
                var urls = imageNodes?.Select(x => x.Attributes["src"]).Select(x => x.Value).ToList() ?? new List<string>();
                //string baseUrl = "https://qpublic.schneidercorp.com/";
                //var urls = htmlDocument.DocumentNode.Descendants("img")
                //                .Select(e => e.GetAttributeValue("src", null))
                //                .Where(s => !string.IsNullOrEmpty(s));
                foreach (var urlOfImage in urls)
                {
                    try
                    {
                        images.Add(new ImageInfo { Image = _webQuery.GetImage(urlOfImage, 1), URL = urlOfImage });
                    }
                    catch { }
                }
            }
            catch { }
            return images;
        }
        private static string GetBaseUrl(string state, string county)
        {
            var georgiaUrls = new Dictionary<string, string> {
                {"Appling","https://qpublic.schneidercorp.com/Application.aspx?AppID=715&LayerID=11428&PageTypeID=2&PageID=4903"},
                {"Athens-Clarke","https://qpublic.schneidercorp.com/Application.aspx?AppID=630&LayerID=11199&PageTypeID=2&PageID=4599"},
                {"Atkinson","https://qpublic.schneidercorp.com/Application.aspx?AppID=634&LayerID=11214&PageTypeID=2&PageID=4611"},
                {"Bacon","https://qpublic.schneidercorp.com/Application.aspx?AppID=711&LayerID=11416&PageTypeID=2&PageID=4883"},
                {"Baldwin","https://qpublic.schneidercorp.com/Application.aspx?AppID=636&LayerID=11985&PageTypeID=2&PageID=5957"},
                {"Banks","https://qpublic.schneidercorp.com/Application.aspx?AppID=782&LayerID=11818&PageTypeID=2&PageID=5689"},
                {"Baker","https://qpublic.schneidercorp.com/Application.aspx?AppID=713&LayerID=11418&PageTypeID=2&PageID=4892" },
                {"Barrow","https://qpublic.schneidercorp.com/Application.aspx?AppID=635&LayerID=11218&PageTypeID=2&PageID=4614"},
                {"Ben Hill","https://qpublic.schneidercorp.com/Application.aspx?AppID=725&LayerID=11766&PageTypeID=2&PageID=5429" },
                { "Bartow","https://qpublic.schneidercorp.com/Application.aspx?AppID=791&LayerID=14444&PageTypeID=2&PageID=7420"},
                {"Berrien","https://qpublic.schneidercorp.com/Application.aspx?AppID=781&LayerID=11817&PageTypeID=2&PageID=5684"},
                {"Bibb","https://qpublic.schneidercorp.com/Application.aspx?AppID=702&LayerID=11410&PageTypeID=2&PageID=4866"},
                {"Bleckley","https://qpublic.schneidercorp.com/Application.aspx?AppID=727&LayerID=11768&PageTypeID=2&PageID=5439"},
                {"Brantley","https://qpublic.schneidercorp.com/Application.aspx?AppID=735&LayerID=11774&PageTypeID=2&PageID=5469"},
                {"Brooks","https://qpublic.schneidercorp.com/Application.aspx?AppID=638&LayerID=12107&PageTypeID=2&PageID=5974"},
                {"Bryan","https://qpublic.schneidercorp.com/Application.aspx?AppID=639&LayerID=11303&PageTypeID=2&PageID=4634"},
                {"Bulloch","https://qpublic.schneidercorp.com/Application.aspx?AppID=637&LayerID=11293&PageTypeID=2&PageID=4626"},
                {"Burke","https://qpublic.schneidercorp.com/Application.aspx?AppID=640&LayerID=11304&PageTypeID=2&PageID=4637"},
                {"Butts","https://qpublic.schneidercorp.com/Application.aspx?AppID=922&LayerID=17901&PageTypeID=2&PageID=7994"},
                {"Calhoun","https://qpublic.schneidercorp.com/Application.aspx?AppID=703&LayerID=11447&PageTypeID=2&PageID=5389"},
                {"Camden","https://qpublic.schneidercorp.com/Application.aspx?AppID=641&LayerID=11309&PageTypeID=2&PageID=4642"},
                {"Candler","https://qpublic.schneidercorp.com/Application.aspx?AppID=799&LayerID=11965&PageTypeID=2&PageID=5936"},
                {"Carroll","https://qpublic.schneidercorp.com/Application.aspx?AppID=663&LayerID=15076&PageTypeID=2&PageID=6798"},
                {"Catoosa","https://qpublic.schneidercorp.com/Application.aspx?AppID=677&LayerID=11364&PageTypeID=2&PageID=4755"},
                {"Charlton","https://qpublic.schneidercorp.com/Application.aspx?AppID=728&LayerID=11769&PageTypeID=2&PageID=5444"},
                {"Chattahoochee","https://qpublic.schneidercorp.com/Application.aspx?AppID=788&LayerID=11820&PageTypeID=2&PageID=5699" },
                {"Chattooga","https://qpublic.schneidercorp.com/Application.aspx?AppID=812&LayerID=14328&PageTypeID=2&PageID=6241" },
                {"Cherokee","https://qpublic.schneidercorp.com/Application.aspx?AppID=992&LayerID=20191&PageTypeID=2&PageID=8794" },
                {"Clay","https://qpublic.schneidercorp.com/Application.aspx?AppID=733&LayerID=11772&PageTypeID=2&PageID=5459"},
                {"Clinch","https://qpublic.schneidercorp.com/Application.aspx?AppID=734&LayerID=11773&PageTypeID=2&PageID=5464"},
                {"Coffee","https://qpublic.schneidercorp.com/Application.aspx?AppID=737&LayerID=11775&PageTypeID=2&PageID=5474"},
                {"Colquitt","https://qpublic.schneidercorp.com/Application.aspx?AppID=665&LayerID=11347&PageTypeID=2&PageID=4711"},
                {"Cook","https://qpublic.schneidercorp.com/Application.aspx?AppID=664&LayerID=11344&PageTypeID=2&PageID=4708"},
                {"Coweta","https://qpublic.schneidercorp.com/Application.aspx?AppID=704&LayerID=11412&PageTypeID=2&PageID=4876"},
                {"Crawford","https://qpublic.schneidercorp.com/Application.aspx?AppID=731&LayerID=11771&PageTypeID=2&PageID=5454"},
                {"Crisp","https://qpublic.schneidercorp.com/Application.aspx?AppID=778&LayerID=11814&PageTypeID=2&PageID=5669"},
                {"Dade","https://qpublic.schneidercorp.com/Application.aspx?AppID=789&LayerID=11821&PageTypeID=2&PageID=5704"},
                {"Dawson","https://qpublic.schneidercorp.com/Application.aspx?AppID=676&LayerID=11636&PageTypeID=2&PageID=5340"},
                {"Decatur","https://qpublic.schneidercorp.com/Application.aspx?AppID=914&LayerID=17623&PageTypeID=2&PageID=7902" },
                {"DeKalb","https://qpublic.schneidercorp.com/Application.aspx?AppID=994&LayerID=20256&PageTypeID=2&PageID=8822" },
                {"Dodge","https://qpublic.schneidercorp.com/Application.aspx?AppID=730&LayerID=11770&PageTypeID=2&PageID=5449"},
                {"Dooly","https://qpublic.schneidercorp.com/Application.aspx?AppID=780&LayerID=11816&PageTypeID=2&PageID=5679"},
                {"Dougherty","https://qpublic.schneidercorp.com/Application.aspx?AppID=762&LayerID=11798&PageTypeID=2&PageID=5589"},
                {"Douglas", "https://qpublic.schneidercorp.com/Application.aspx?AppID=988&LayerID=20162&PageTypeID=2&PageID=8760"},
                {"Early","https://qpublic.schneidercorp.com/Application.aspx?AppID=741&LayerID=11778&PageTypeID=2&PageID=5489" },
                {"Echols","https://qpublic.schneidercorp.com/Application.aspx?AppID=765&LayerID=11801&PageTypeID=2&PageID=5604"},
                {"Effingham","https://qpublic.schneidercorp.com/Application.aspx?AppID=666&LayerID=11348&PageTypeID=2&PageID=4714"},
                {"Elbert","https://qpublic.schneidercorp.com/Application.aspx?AppID=667&LayerID=11830&PageTypeID=2&PageID=5729"},
                {"Emanuel","https://qpublic.schneidercorp.com/Application.aspx?AppID=668&LayerID=11350&PageTypeID=2&PageID=4720"},
                {"Evans","https://qpublic.schneidercorp.com/Application.aspx?AppID=740&LayerID=11777&PageTypeID=2&PageID=5484"},
                {"Fannin","https://qpublic.schneidercorp.com/Application.aspx?AppID=714&LayerID=11449&PageTypeID=2&PageID=5402"},
                {"Fayette","https://qpublic.schneidercorp.com/Application.aspx?AppID=942&LayerID=18406&PageTypeID=2&PageID=8204" },
                {"Floyd","https://qpublic.schneidercorp.com/Application.aspx?AppID=802&LayerID=13374&PageTypeID=2&PageID=6037"},
                {"Forsyth","https://qpublic.schneidercorp.com/Application.aspx?AppID=1027&LayerID=21667&PageTypeID=2&PageID=9228"},
                {"Franklin","https://qpublic.schneidercorp.com/Application.aspx?AppID=831&LayerID=15012&PageTypeID=2&PageID=6777"},
                {"Fulton","https://qpublic.schneidercorp.com/Application.aspx?AppID=936&LayerID=18251&PageTypeID=2&PageID=8154"},
                {"Gilmer","https://qpublic.schneidercorp.com/Application.aspx?AppID=672&LayerID=11357&PageTypeID=2&PageID=4736"},
                {"Glascock","https://qpublic.schneidercorp.com/Application.aspx?AppID=669&LayerID=11353&PageTypeID=2&PageID=4725"},
                {"Glynn","https://qpublic.schneidercorp.com/Application.aspx?AppID=964&LayerID=19142&PageTypeID=2&PageID=8446"},
                {"Gordon","https://qpublic.schneidercorp.com/Application.aspx?AppID=629&LayerID=11198&PageTypeID=2&PageID=4596"},
                {"Grady","https://qpublic.schneidercorp.com/Application.aspx?AppID=742&LayerID=11779&PageTypeID=2&PageID=5494"},
                {"Greene","https://qpublic.schneidercorp.com/Application.aspx?AppID=698&LayerID=11403&PageTypeID=2&PageID=4850"},
                {"Habersham","https://qpublic.schneidercorp.com/Application.aspx?AppID=1010&LayerID=20413&PageTypeID=2&PageID=8924"},
                {"Hall","https://qpublic.schneidercorp.com/Application.aspx?AppID=724&LayerID=11765&PageTypeID=2&PageID=5424"},
                {"Hancock","https://qpublic.schneidercorp.com/Application.aspx?AppID=754&LayerID=11790&PageTypeID=2&PageID=5549"},
                {"Haralson","https://qpublic.schneidercorp.com/Application.aspx?AppID=744&LayerID=11781&PageTypeID=2&PageID=5504"},
                {"Harris","https://qpublic.schneidercorp.com/Application.aspx?AppID=700&LayerID=11408&PageTypeID=2&PageID=4858"},
                {"Hart","https://qpublic.schneidercorp.com/Application.aspx?AppID=751&LayerID=11787&PageTypeID=2&PageID=5534"},
                {"Heard","https://qpublic.schneidercorp.com/Application.aspx?AppID=701&LayerID=11409&PageTypeID=2&PageID=4862"},
                {"Henry", "https://qpublic.schneidercorp.com/Application.aspx?AppID=1035&LayerID=22139&PageTypeID=2&PageID=9366"},
                {"Houston","https://qpublic.schneidercorp.com/Application.aspx?AppID=671&LayerID=11356&PageTypeID=2&PageID=4731"},
                {"Irwin","https://qpublic.schneidercorp.com/Application.aspx?AppID=763&LayerID=11799&PageTypeID=2&PageID=5594" },
                {"Jackson","https://qpublic.schneidercorp.com/Application.aspx?AppID=797&LayerID=11838&PageTypeID=2&PageID=5760"},
                {"Jasper","https://qpublic.schneidercorp.com/Application.aspx?AppID=699&LayerID=11404&PageTypeID=2&PageID=4854"},
                {"Jeff Davis","https://qpublic.schneidercorp.com/Application.aspx?AppID=743&LayerID=11780&PageTypeID=2&PageID=5499"},
                {"Jefferson","https://qpublic.schneidercorp.com/Application.aspx?AppID=670&LayerID=11355&PageTypeID=2&PageID=4728"},
                {"Jenkins","https://qpublic.schneidercorp.com/Application.aspx?AppID=745&LayerID=11782&PageTypeID=2&PageID=5509"},
                {"Johnson","https://qpublic.schneidercorp.com/Application.aspx?AppID=692&LayerID=11390&PageTypeID=2&PageID=4821"},
                {"Jones","https://qpublic.schneidercorp.com/Application.aspx?AppID=764&LayerID=11800&PageTypeID=2&PageID=5599"},
                {"Lamar","https://qpublic.schneidercorp.com/Application.aspx?AppID=749&LayerID=11785&PageTypeID=2&PageID=5524"},
                {"Lanier","https://qpublic.schneidercorp.com/Application.aspx?AppID=750&LayerID=11786&PageTypeID=2&PageID=5529"},
                {"Laurens","https://qpublic.schneidercorp.com/Application.aspx?AppID=696&LayerID=11398&PageTypeID=2&PageID=4842"},
                {"Lee","https://qpublic.schneidercorp.com/Application.aspx?AppID=563&LayerID=8424&PageTypeID=2&PageID=4082"},
                {"Liberty","https://qpublic.schneidercorp.com/Application.aspx?AppID=989&LayerID=20166&PageTypeID=2&PageID=8770"},
                {"Lincoln","https://qpublic.schneidercorp.com/Application.aspx?AppID=675&LayerID=11362&PageTypeID=2&PageID=4745"},
                {"Long","https://qpublic.schneidercorp.com/Application.aspx?AppID=748&LayerID=11784&PageTypeID=2&PageID=5519" },
                {"Lowndes","https://qpublic.schneidercorp.com/Application.aspx?AppID=631&LayerID=11201&PageTypeID=2&PageID=4602"},
                {"Lumpkin","https://qpublic.schneidercorp.com/Application.aspx?AppID=991&LayerID=20168&PageTypeID=2&PageID=8780"},
                {"Macon","https://qpublic.schneidercorp.com/Application.aspx?AppID=747&LayerID=11783&PageTypeID=2&PageID=5514" },
                {"Madison","https://qpublic.schneidercorp.com/Application.aspx?AppID=716&LayerID=11429&PageTypeID=2&PageID=4907"},
                {"Marion","https://qpublic.schneidercorp.com/Application.aspx?AppID=990&LayerID=20167&PageTypeID=2&PageID=8776"},
                {"McDuffie","https://qpublic.schneidercorp.com/Application.aspx?AppID=712&LayerID=11417&PageTypeID=2&PageID=4888"},
                {"McIntosh","https://qpublic.schneidercorp.com/Application.aspx?AppID=717&LayerID=11430&PageTypeID=2&PageID=4913"},
                {"Meriwether","https://qpublic.schneidercorp.com/Application.aspx?AppID=775&LayerID=11811&PageTypeID=2&PageID=5654"},
                {"Miller","https://qpublic.schneidercorp.com/Application.aspx?AppID=755&LayerID=11791&PageTypeID=2&PageID=5554"},
                {"Mitchell","https://qpublic.schneidercorp.com/Application.aspx?AppID=937&LayerID=18309&PageTypeID=2&PageID=8169" },
                {"Monroe","https://qpublic.schneidercorp.com/Application.aspx?AppID=757&LayerID=11793&PageTypeID=2&PageID=5564"},
                {"Montgomery","https://qpublic.schneidercorp.com/Application.aspx?AppID=776&LayerID=11812&PageTypeID=2&PageID=5659"},
                {"Morgan","https://qpublic.schneidercorp.com/Application.aspx?AppID=697&LayerID=11400&PageTypeID=2&PageID=4846"},
                {"Murray","https://qpublic.schneidercorp.com/Application.aspx?AppID=756&LayerID=11792&PageTypeID=2&PageID=5559"},
                {"Newton","https://qpublic.schneidercorp.com/Application.aspx?AppID=794&LayerID=11825&PageTypeID=2&PageID=5724"},
                {"Oconee","https://qpublic.schneidercorp.com/Application.aspx?AppID=686&LayerID=11376&PageTypeID=2&PageID=4791"},
                {"Oglethorpe","https://qpublic.schneidercorp.com/Application.aspx?AppID=758&LayerID=11794&PageTypeID=2&PageID=5569"},
                {"Quitman","https://qpublic.schneidercorp.com/Application.aspx?AppID=726&LayerID=11767&PageTypeID=2&PageID=5434" },
                {"Paulding","https://qpublic.schneidercorp.com/Application.aspx?AppID=689&LayerID=11378&PageTypeID=2&PageID=4800"},
                {"Peach","https://qpublic.schneidercorp.com/Application.aspx?AppID=783&LayerID=11819&PageTypeID=2&PageID=5694"},
                {"Pickens","https://qpublic.schneidercorp.com/Application.aspx?AppID=627&LayerID=11193&PageTypeID=2&PageID=4590"},
                {"Pierce","https://qpublic.schneidercorp.com/Application.aspx?AppID=683&LayerID=11373&PageTypeID=2&PageID=4779"},
                {"Pike","https://qpublic.schneidercorp.com/Application.aspx?AppID=759&LayerID=11795&PageTypeID=2&PageID=5574"},
                {"Polk","https://qpublic.schneidercorp.com/Application.aspx?AppID=690&LayerID=11379&PageTypeID=2&PageID=4804"},
                {"Pulaski","https://qpublic.schneidercorp.com/Application.aspx?AppID=760&LayerID=11796&PageTypeID=2&PageID=5579"},
                {"Putnam","https://qpublic.schneidercorp.com/Application.aspx?AppID=761&LayerID=11797&PageTypeID=2&PageID=5584"},
                {"Rabun","https://qpublic.schneidercorp.com/Application.aspx?AppID=674&LayerID=11359&PageTypeID=2&PageID=4742"},
                {"Randolph","https://qpublic.schneidercorp.com/Application.aspx?AppID=804&LayerID=13375&PageTypeID=2&PageID=6044"},
                {"Richmond","https://qpublic.schneidercorp.com/Application.aspx?AppID=678&LayerID=11365&PageTypeID=2&PageID=4758"},
                {"Rockdale","https://qpublic.schneidercorp.com/Application.aspx?AppID=694&LayerID=11394&PageTypeID=2&PageID=4832"},
                {"Schley","https://qpublic.schneidercorp.com/Application.aspx?AppID=793&LayerID=11824&PageTypeID=2&PageID=5719"},
                {"Screven","https://qpublic.schneidercorp.com/Application.aspx?AppID=673&LayerID=11358&PageTypeID=2&PageID=4739"},
                {"Seminole","https://qpublic.schneidercorp.com/Application.aspx?AppID=790&LayerID=11822&PageTypeID=2&PageID=5709"},
                {"Spalding","https://qpublic.schneidercorp.com/Application.aspx?AppID=766&LayerID=11802&PageTypeID=2&PageID=5609"},
                {"Stephens","https://qpublic.schneidercorp.com/Application.aspx?AppID=779&LayerID=11815&PageTypeID=2&PageID=5674"},
                {"Stewart","https://qpublic.schneidercorp.com/Application.aspx?AppID=767&LayerID=11803&PageTypeID=2&PageID=5614"},
                {"Sumter","https://qpublic.schneidercorp.com/Application.aspx?AppID=849&LayerID=15775&PageTypeID=2&PageID=7041"},
                {"Talbot","https://qpublic.schneidercorp.com/Application.aspx?AppID=680&LayerID=11370&PageTypeID=2&PageID=4766"},
                {"Taliaferro", "https://qpublic.schneidercorp.com/Application.aspx?AppID=798&LayerID=11839&PageTypeID=2&PageID=5765" },
                {"Tattnall","https://qpublic.schneidercorp.com/Application.aspx?AppID=681&LayerID=11371&PageTypeID=2&PageID=4770"},
                {"Taylor","https://qpublic.schneidercorp.com/Application.aspx?AppID=769&LayerID=11805&PageTypeID=2&PageID=5624"},
                {"Telfair","https://qpublic.schneidercorp.com/Application.aspx?AppID=809&LayerID=14326&PageTypeID=2&PageID=6227"},
                {"Terrell","https://qpublic.schneidercorp.com/Application.aspx?AppID=770&LayerID=11806&PageTypeID=2&PageID=5629"},
                {"Thomas","https://qpublic.schneidercorp.com/Application.aspx?AppID=682&LayerID=11372&PageTypeID=2&PageID=4775"},
                {"Tift","https://qpublic.schneidercorp.com/Application.aspx?AppID=691&LayerID=11381&PageTypeID=2&PageID=4811"},
                {"Toombs","https://qpublic.schneidercorp.com/Application.aspx?AppID=768&LayerID=11804&PageTypeID=2&PageID=5619"},
                {"Towns","https://qpublic.schneidercorp.com/Application.aspx?AppID=846&LayerID=15440&PageTypeID=2&PageID=7008" },
                {"Treutlen","https://qpublic.schneidercorp.com/Application.aspx?AppID=811&LayerID=14327&PageTypeID=2&PageID=6234"},
                {"Troup","https://qpublic.schneidercorp.com/Application.aspx?AppID=633&LayerID=18434&PageTypeID=2&PageID=8220"},
                {"Turner","https://qpublic.schneidercorp.com/Application.aspx?AppID=771&LayerID=11807&PageTypeID=2&PageID=5634"},
                {"Twiggs","https://qpublic.schneidercorp.com/Application.aspx?AppID=684&LayerID=11374&PageTypeID=2&PageID=4783"},
                {"Union","https://qpublic.schneidercorp.com/Application.aspx?AppID=773&LayerID=11809&PageTypeID=2&PageID=5644"},
                {"Upson","https://qpublic.schneidercorp.com/Application.aspx?AppID=562&LayerID=8423&PageTypeID=2&PageID=4079"},
                {"Walker","https://qpublic.schneidercorp.com/Application.aspx?AppID=695&LayerID=11397&PageTypeID=2&PageID=4838"},
                {"Walton","https://qpublic.schneidercorp.com/Application.aspx?AppID=628&LayerID=11921&PageTypeID=2&PageID=5799"},
                {"Ware","https://qpublic.schneidercorp.com/Application.aspx?AppID=719&LayerID=11432&PageTypeID=2&PageID=4924"},
                {"Warren","https://qpublic.schneidercorp.com/Application.aspx?AppID=772&LayerID=11808&PageTypeID=2&PageID=5639"},
                {"Washington","https://qpublic.schneidercorp.com/Application.aspx?AppID=720&LayerID=11437&PageTypeID=2&PageID=4928"},
                {"Wayne","https://qpublic.schneidercorp.com/Application.aspx?AppID=850&LayerID=15800&PageTypeID=2&PageID=7062"},
                {"Webster","https://qpublic.schneidercorp.com/Application.aspx?AppID=774&LayerID=11810&PageTypeID=2&PageID=5649"},
                {"Wheeler","https://qpublic.schneidercorp.com/Application.aspx?AppID=777&LayerID=11813&PageTypeID=2&PageID=5664"},
                {"White","https://qpublic.schneidercorp.com/Application.aspx?AppID=982&LayerID=19945&PageTypeID=2&PageID=8692"},
                {"Wilcox","https://qpublic.schneidercorp.com/Application.aspx?AppID=939&LayerID=18333&PageTypeID=2&PageID=8178" },
                {"Wilkes","https://qpublic.schneidercorp.com/Application.aspx?AppID=693&LayerID=11393&PageTypeID=2&PageID=4828"},
                {"Wilkinson","https://qpublic.schneidercorp.com/Application.aspx?AppID=718&LayerID=11431&PageTypeID=2&PageID=4919"},
                {"Worth","https://qpublic.schneidercorp.com/Application.aspx?AppID=721&LayerID=11764&PageTypeID=2&PageID=5419"}};

            return georgiaUrls.FirstOrDefault(x => x.Key.ToLower().Trim() == county.ToLower().Trim()).Value;
        }

        public HtmlDocument DoParcelSearch(string state, string county, string parcelNumber)
        {
            var baseUrl = GetBaseUrl(state, county);

            baseUrl = $"{baseUrl}&KeyValue={parcelNumber}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl);

            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.KeepAlive = true;

            request.CookieContainer = new CookieContainer();
            var cookieStr = @"__utmz=184342052.1494507410.1.1.utmccn=(referral)|utmcsr=qpublic.net|utmcct=/|utmcmd=referral; __utma=184342052.1096614886.1494507410.1494507410.1494587023.2; ASP.NET_SessionId=mpkeoxnjwk4yneetcijjunh3; _ga=GA1.2.967251908.1493209546; _gid=GA1.2.168269956.1495456800";
            request.CookieContainer.SetCookies(new Uri(baseUrl), cookieStr);

            request.Referer = baseUrl;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            request.AllowAutoRedirect = true;
            var stringData = $"__EVENTTARGET=ctlBodyPane%24ctl02%24ctl01%24btnSearch&__EVENTARGUMENT=&__VIEWSTATE=np7zoSzAVtfIQEBhg%2BTp9xLqxXz27teS0jzYh3wlM9%2F2wOa8pwRpxKwzv0jPB0A%2Bu5leXhmP24yn17lulI7UXpacmB9HSY126qvPBAIwYGA%2FRRcfceZrXqf4PnByJUTr0f39UjuWEsconm7c3Z2K7BjTe6eyC%2FxuhJhkPZASur26Hui4ZbzriAmWufaBuNSgY%2BjKUIbI0VjT%2BYhgMIBUqJKvDN76tFfjHfQU2MsJhjmEXFsg&__VIEWSTATEGENERATOR=569DB96F&ctlBodyPane%24ctl00%24ctl01%24txtName=&ctlBodyPane%24ctl01%24ctl01%24txtAddress=&ctlBodyPane%24ctl02%24ctl01%24txtParcelID={HttpUtility.UrlEncode(parcelNumber)}&ctlBodyPane%24ctl03%24ctl01%24txtAlternateID=&ctlBodyPane%24ctl04%24ctl01%24txtName=&ctlBodyPane%24ctl05%24ctl01%24srch1%24txtInput=";
            var data = Encoding.ASCII.GetBytes(stringData);

            using (var newStream = request.GetRequestStream())
            {
                newStream.Write(data, 0, data.Length);
            }

            var response = request.GetResponse();

            HtmlDocument doc = new HtmlDocument();
            doc.Load(response.GetResponseStream());

            return doc;
        }
        protected static bool IsSearchResultPage(HtmlDocument doc)
        {
            return doc.DocumentNode.OuterHtml.Contains("Search Results");
        }
        public virtual List<ImageInfo> GetImages(string state, string county, string parcelNumber)
        {
            List<ImageInfo> imageInfos = new List<ImageInfo>();
            try
            {
                var doc = DoParcelSearch(state, county, parcelNumber);
                var pageUrl = "";
                if (IsSearchResultPage(doc))
                {
                    var table = doc.DocumentNode.SelectNodes("//table").Where(x => x.Id.Contains("gvwParcelResults")).FirstOrDefault();

                    var lnk = table.SelectNodes("//table//tbody//tr//td//a").FirstOrDefault(x => x.Id.Contains("lnkParcelID")
                    && x.InnerText.Replace("\r\n", "").Replace(" ", "") == parcelNumber

                    );
                    if (lnk != null && lnk.Attributes["href"] != null)
                    {
                        pageUrl = lnk.Attributes["href"].Value;
                    }

                    var str0 = lnk.GetAttributeValue("href", string.Empty);


                    /*var rows = table.SelectNodes("//table//tbody//tr");
                    foreach (var row in rows)
                    {
                      var cells=  row.SelectNodes("//td");
                        if (cells == null || cells.Count < 2) continue;
                        if (cells[1].InnerText.Replace("\r\n","").Replace(" ", "") != parcelNumber) continue;
                        var link=cells[1].SelectNodes("//a").Where(x=>x.Id.Contains("lnkParcelID")).FirstOrDefault();
                        if (link == null) continue;
                        var href = link.Attributes["href"];
                        if (href != null)
                        {
                            pageUrl = href.Value;
                            break;
                        }
                    }*/

                    if (string.IsNullOrWhiteSpace(pageUrl))
                    {
                        return new List<ImageInfo>();
                    }

                    var str1 = HttpUtility.UrlDecode(pageUrl);
                    str1 = str1.Replace("amp;", "");
                    var str2 = HttpUtility.UrlEncode(str1);

                    doc = _webQuery.GetSource($"https://qpublic.schneidercorp.com{str1}", 1);

                }

                var imageNodes = doc.DocumentNode.SelectNodes("//div[@id='photogrid']//div[@class='photo-thumbnail']//img");

                var urls = imageNodes?.Select(x => x.Attributes["src"]).Select(x => x.Value).ToList() ?? new List<string>();
                for (int i = 0; i < urls.Count; i++)
                {
                    try
                    {
                        var image = _webQuery.GetImage(urls[i], 1);
                        imageInfos.Add(new ImageInfo() { Image = image, Name = parcelNumber, URL = urls[i] });
                    }
                    catch { }
                }
            }
            catch { }
            return imageInfos;
        }
    }
}