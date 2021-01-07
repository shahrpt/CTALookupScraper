using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace CTALookup.Scrapers
{
    public class ScraperQpublicNew : Scraper
    {
        protected string BaseUrl = "https://qpublic.schneidercorp.com/";
        public string CountyCode;
        protected string SearchUrl;
        protected string SubmitUrl;
        protected string XpathLinkNodes;

        public ScraperQpublicNew(string countyCode = "")
        {
            CountyCode = countyCode;
        }

        protected virtual string GetParameters(string parcelNumber, bool addCountyCode = true)
        {
            var dict = new Dictionary<string, string>
            {
                {"BEGIN", "0"},
                {"INPUT", parcelNumber},
                {"searchType", "parcel_id"},
                {"Parcel_Search", "Search By Parcel ID"}
            };
            if (addCountyCode)
            {
                dict.Add("county", CountyCode);
            }
            return WebQuery.GetStringFromParameters(dict);
        }

        protected static bool NoRecordsFound(HtmlDocument doc)
        {
            return doc.DocumentNode.OuterHtml.Contains("No Records Found");
        }

        protected HtmlDocument SubmitQuery(string parameters)
        {
            //return _webQuery.GetSource(SubmitUrl + "?" + parameters, 1, SearchUrl);
            return _webQuery.GetPost(SubmitUrl, parameters, 1, SearchUrl);

        }

        protected HtmlDocument Search(string parcelNumber, bool addCountyCode = true, bool useShortNotation = false)
        {
            var baseUrl = GetBaseUrl(string.Empty, CountyCode);

            //baseUrl = $"{baseUrl}&KeyValue={parcelNumber}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseUrl);

            //request.Proxy = new WebProxy("127.0.0.1", 8888);


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

            var stringData = string.Empty;

            if (useShortNotation)
            {
                stringData = $"__EVENTTARGET=ctlBodyPane%24ctl01%24ctl01%24btnSearch&__EVENTARGUMENT=&__VIEWSTATE=c6ngTNkKCp2BkniktsTh0JEb5ltDebNozEK9vVN%2BTg2VEwXAjDw4NsMxBPrUwibXw02F2CiXqHSW5XIi7FNlbEnfTEqRDhWX4%2F2xL2cXgYk7ulTbp1d3%2Fq0wqWTxrzCCvHFSha5Fb1CNMYDJP8PkqhrDFNV1gNBtt7Nf4L1DjipHvMzTIZD%2FyIHfCpcJSlUXhnFqw3jISnDMykUVDj1%2F4DsVIFPPTYp7NOctfUmbugNS%2F08I&__VIEWSTATEGENERATOR=569DB96F&ctlBodyPane%24ctl00%24ctl01%24txtName=&ctlBodyPane%24ctl00%24ctl01%24txtNameExact=&ctlBodyPane%24ctl01%24ctl01%24txtParcelID={HttpUtility.UrlEncode(parcelNumber)}&ctlBodyPane%24ctl02%24ctl01%24txtAddress=&ctlBodyPane%24ctl02%24ctl01%24txtAddressExact=&ctlBodyPane%24ctl03%24ctl01%24ddlSubdivision=";
            }
            else
            {
                stringData = $"__EVENTTARGET=ctlBodyPane%24ctl02%24ctl01%24btnSearch&__EVENTARGUMENT=&__VIEWSTATE=np7zoSzAVtfIQEBhg%2BTp9xLqxXz27teS0jzYh3wlM9%2F2wOa8pwRpxKwzv0jPB0A%2Bu5leXhmP24yn17lulI7UXpacmB9HSY126qvPBAIwYGA%2FRRcfceZrXqf4PnByJUTr0f39UjuWEsconm7c3Z2K7BjTe6eyC%2FxuhJhkPZASur26Hui4ZbzriAmWufaBuNSgY%2BjKUIbI0VjT%2BYhgMIBUqJKvDN76tFfjHfQU2MsJhjmEXFsg&__VIEWSTATEGENERATOR=569DB96F&ctlBodyPane%24ctl02%24ctl01%24txtParcelID={HttpUtility.UrlEncode(parcelNumber)}";
            }

            var data = Encoding.ASCII.GetBytes(stringData);

            using (var newStream = request.GetRequestStream())
            {
                newStream.Write(data, 0, data.Length);
            }

            var response = request.GetResponse();

            HtmlDocument doc = new HtmlDocument();
            doc.Load(response.GetResponseStream());

            if (doc.DocumentNode.InnerHtml.Contains("Search Results") || doc.DocumentNode.InnerHtml.Contains("Parcel Results"))
            {
                doc = LoadFromDirectUrl(doc);
            }
            else if (!doc.DocumentNode.InnerHtml.Contains("Summary") && useShortNotation == false)
            {
                doc = Search(parcelNumber, useShortNotation: true);
            }

            return doc;
        }

        private HtmlDocument LoadFromDirectUrl(HtmlDocument doc)
        {
            var result = doc;

            var directUrlNode = doc.DocumentNode.SelectSingleNode("//a[@id='ctlBodyPane_ctl00_ctl01_gvwParcelResults_ctl02_lnkParcelID']");


            if (directUrlNode != null)
            {
                var directUrl = WebQuery.BuildUrl(directUrlNode.Attributes["href"].Value, BaseUrl).Replace("amp;", "");


                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(directUrl);

                //request.Proxy = new WebProxy("127.0.0.1", 8888);


                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "GET";
                request.KeepAlive = true;

                request.CookieContainer = new CookieContainer();
                var cookieStr = @"__utmz=184342052.1494507410.1.1.utmccn=(referral)|utmcsr=qpublic.net|utmcct=/|utmcmd=referral; __utma=184342052.1096614886.1494507410.1494507410.1494587023.2; ASP.NET_SessionId=mpkeoxnjwk4yneetcijjunh3; _ga=GA1.2.967251908.1493209546; _gid=GA1.2.168269956.1495456800";
                request.CookieContainer.SetCookies(new Uri(directUrl), cookieStr);

                request.Referer = directUrl;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AllowAutoRedirect = true;

                var response = request.GetResponse();
                result.Load(response.GetResponseStream());


            }

            return result;
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

        public virtual List<string> GetImages(string state, string county, string parcelNumber)
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
                    return new List<string>();
                }

                var str1 = HttpUtility.UrlDecode(pageUrl);
                str1 = str1.Replace("amp;", "");
                var str2 = HttpUtility.UrlEncode(str1);

                doc = _webQuery.GetSource($"https://qpublic.schneidercorp.com{str1}", 1);

            }

            var imageNodes = doc.DocumentNode.SelectNodes("//div[@id='photogrid']//div[@class='photo-thumbnail']//img");

            return imageNodes?.Select(x => x.Attributes["src"]).Select(x => x.Value).ToList() ?? new List<string>();
        }

        protected virtual string GetLink(HtmlDocument doc)
        {
            //            var linkNodes = doc.DocumentNode.SelectNodes(XpathLinkNodes);
            //            if (linkNodes == null)
            //            {
            //                LogThrowNotLinkFound(doc);
            //            }
            //            string link = linkNodes[0].SelectSingleNode(".//a[@href]").Attributes["href"].Value;
            //            link = WebQuery.BuildUrl(link, BaseUrl);
            //            return link;

            var node = doc.DocumentNode.SelectSingleNode("//td[@class='search_value'][1]/a");
            if (node == null)
            {
                LogThrowNotLinkFound(doc);
            }
            var link = WebQuery.BuildUrl(node.Attributes["href"].Value, BaseUrl);

            return link;
        }

        protected static string GetValueOfField(HtmlDocument doc, string fieldName, bool ommitIfNotExists = true)
        {
            var node = GetNodeOfField(doc, fieldName);
            if (node == null)
            {
                if (ommitIfNotExists)
                {
                    return "n/a";
                }
                throw new Exception(string.Format("Error getting value of field {0}", fieldName));
            }

            return WebQuery.Clean(node.InnerText).Replace("\n", "");
        }

        protected static string GetValueOfFieldWithFailover(HtmlDocument doc, string fieldName, string failoverFieldName = "")
        {
            var node = GetNodeOfField(doc, fieldName);

            if (failoverFieldName != "" && (node == null || WebQuery.Clean(node.InnerText).Replace("\n", "") == ""))
            {
                node = GetNodeOfField(doc, failoverFieldName);
            }

            return node == null ? "n/a" : WebQuery.Clean(node.InnerText).Replace("\n", "");
        }

        protected static HtmlNode GetNodeOfField(HtmlDocument doc, string fieldName)
        {
            // //*[@id="ctlBodyPane_ctl00_ctl01_dvNonPrebillMH"]/table/tbody/tr[1]/td[1]/strong


            return (from n in doc.DocumentNode.SelectNodes("//td/strong")
                    where n.InnerText.Contains(fieldName)
                    select n.ParentNode.NextSibling.Name == "#text" ? n.ParentNode.NextSibling.NextSibling : n.ParentNode.NextSibling).FirstOrDefault();

            // return doc.DocumentNode.SelectNodes($"//td/strong[text()[contains(., '{fieldName}')]]").FirstOrDefault().ParentNode.NextSibling.NextSibling;
        }

        protected static string GetValueValueOfField(HtmlDocument doc, string fieldName, bool ommitIfNotExists = true)
        {
            var node = GetNodeOfValueField(doc, fieldName);
            if (node == null)
            {
                if (ommitIfNotExists)
                {
                    return "n/a";
                }
                throw new Exception(string.Format("Error getting value of field {0}", fieldName));
            }

            return WebQuery.Clean(node.InnerText).Replace("\n", "");
        }

        protected static HtmlNode GetNodeOfValueField(HtmlDocument doc, string fieldName)
        {
            // //*[@id="ctlBodyPane_ctl00_ctl01_dvNonPrebillMH"]/table/tbody/tr[1]/td[1]/strong

            var value = (from n in doc.DocumentNode.SelectNodes("//td")
                         where n.InnerText.Contains(fieldName)
                         select n.NextSibling.Name == "#text" ? n.NextSibling.NextSibling : n.NextSibling).FirstOrDefault();
            if (value == null)
            {
                value = (from n in doc.DocumentNode.SelectNodes("//th")
                         where n.InnerText.Contains(fieldName)
                         select n.NextSibling.Name == "#text" ? n.NextSibling.NextSibling : n.NextSibling).FirstOrDefault();
            }

            return value;

            // return doc.DocumentNode.SelectNodes($"//td/strong[text()[contains(., '{fieldName}')]]").FirstOrDefault().ParentNode.NextSibling.NextSibling;
        }

        protected static string CleanValue(string text)
        {
            try
            {
                return Regex.Replace(text, @"(.*?)(\s+)(.*)",
                    m => m.Groups[1].Value + " " + m.Groups[3].Value);
            }
            catch
            {
                return text;
            }
        }

        protected string GetValueFromPreliminaryValueTable(HtmlDocument doc, int columnIndex, bool ommitIfNotExists = true)
        {
            try
            {
                var taxValuesNodes = doc.DocumentNode.SelectNodes("//td[@class='tax_value']");
                //The Land Value is the first one:
                if (taxValuesNodes == null)
                {
                    LogMessageCodeAndThrowException("Error getting land value", doc.DocumentNode.OuterHtml);
                }
                if (columnIndex >= taxValuesNodes.Count)
                {
                    LogMessageCodeAndThrowException("Error getting land value. Field is greather",
                        doc.DocumentNode.OuterHtml);
                }
                var result = WebQuery.Clean(taxValuesNodes[columnIndex].InnerText);
                return CleanValue(result);
            }
            catch
            {
                if (ommitIfNotExists)
                {
                    return "n/a";
                }
                throw;
            }
        }

        protected void AssignOwnerAddress(Item item, HtmlDocument doc, string regex = "(.+), (.+) (.+)")
        {
            var addressData = GetOwnerAddress(doc);

            var name = Utils.CleanMultipleSpaces(item.OwnerName + " " + addressData.co);
            AssignNames(item, name);

            AssignCityStateZipToOwnerAddress(item, addressData.address, regex);

            item.OwnerAddress = Utils.CleanMultipleSpaces(addressData.po);
            //SplitLinealOwnerAddress(item);
        }

        protected (string co, string po, string address) GetOwnerAddress(HtmlDocument doc)
        {
            var co = string.Empty;
            var po = string.Empty;
            var address = string.Empty;

            var addressNode = GetOwnerAddressNode(doc);
            var stateCityZipNode = GetOwnerStateCityZipNode(doc);

            var name = GetOwnerNameAndAddressNode(doc);

            if (name != null)
            {
                var items = name.InnerHtml.Split(new[] { "<br>", "<br/>" }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Replace("\r\n", string.Empty)).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();

                if (items != null && items.Count == 4)
                {
                    co = items[1];
                    po = items[2];
                    address = items[3];
                }
                else if (items != null && items.Count == 3)
                {
                    po = items[1];
                    address = items[2];
                }

                //if (items != null && items.Count > 0)
                //{
                //    co = items[1].Contains("c/o") ? items[1] : string.Empty;
                //    po = items[1].Contains("c/o") ? items[2] : items[1];
                //    address = items[1].Contains("c/o") ? items[3] : items[2];
                //}
            }
            else if (addressNode != null)
            {
                if (addressNode.InnerText.ToLower().Contains("c/o"))
                {
                    var split = addressNode.InnerHtml.Trim().Split(new[] { "<br>", "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
                    co = split[0].Trim();
                    po = split[1].Trim();
                }
                else
                {
                    po = addressNode.InnerText.Trim();
                }

                address = stateCityZipNode.InnerText.Trim();
            }

            return (co, po, address);
        }

        public virtual string GetVerticalValueFromQpublic(HtmlDocument doc, params string[] fieldNames)
        {
            HtmlNode tdNode = null;
            doc.OptionAutoCloseOnEnd = true;

            IList<string> fields = fieldNames.ToList();
            foreach (var f in fieldNames)
            {
                fields.Insert(0, f.Replace(" ", ""));
            }

            foreach (var fieldName in fields)
            {
                tdNode =
                    doc.DocumentNode.SelectNodes("//td")
                        .FirstOrDefault(
                            x =>
                                x.SelectSingleNode(".//td") == null &&
                                WebQuery.Clean(x.InnerText).ToLower().Contains(fieldName.ToLower()));
                if (tdNode != null)
                {
                    break;
                }
            }

            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            if (trNode == null)
            {
                return "n/a";
            }

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);

            if (trNode.NextSibling == null)
            {
                trNode = trNode.SelectSingleNode("./tr");
            }
            else
            {
                trNode = trNode.NextSibling;

                while (trNode.Name.ToLower() != "tr")
                {
                    trNode = trNode.NextSibling;
                    if (trNode == null)
                    {
                        return "n/a";
                    }
                }
            }

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);

            return Regex.Replace(value, @"\s+", " ");
        }

        public virtual string GetVerticalValueFromQpublicEx(HtmlDocument doc, params string[] fieldNames)
        {
            HtmlNode tdNode = null;
            doc.OptionAutoCloseOnEnd = true;

            var nodes = doc.DocumentNode.SelectNodes("//td");
            foreach (var node in nodes)
            {
                var textNodes = node.SelectNodes("text() | */text()");
                if (textNodes == null || textNodes.Count == 0)
                {
                    continue;
                }
                var texts = textNodes.Select(x => WebQuery.Clean(x.InnerText?.Replace("<br/>", "")));
                var text = string.Join(" ", texts);
                foreach (var fieldName in fieldNames)
                {
                    if (!text.ToLower().Contains(fieldName.ToLower()))
                    {
                        continue;
                    }

                    if (node.Ancestors("tr").FirstOrDefault() == null ||
                        node.Ancestors("tr").First().Ancestors("table").FirstOrDefault() == null)
                    {
                        continue;
                    }
                    var table = node.Ancestors("tr").First().Ancestors("table").First();
                    if (table.SelectNodes("tr").IndexOf(node.Ancestors("tr").First()) == 2)
                    {
                        tdNode = node;
                        break;
                    }
                }
                if (tdNode != null)
                {
                    break;
                }
            }
            //      .FirstOrDefault(
            //          x => x.SelectSingleNode(".//td") == null && WebQuery.Clean(x.InnerText).ToLower().Contains(fieldName.ToLower()));
            /*
            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);


            if (trNode.NextSibling == null)
            {
                trNode = trNode.SelectSingleNode("./tr");
            }
            else
            {
                trNode = trNode.NextSibling;
                while (trNode.Name.ToLower() != "tr")
                {
                    var tmp = trNode.NextSibling;
                    if (tmp == null)
                    {
                        break;
                    }
                    trNode = tmp;
                }
            }

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes == null || Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);
            +
            return Regex.Replace(value, @"\s+", " ");*/
            if (tdNode == null)
            {
                return "n/a";
            }

            var trNode = tdNode.Ancestors("tr").First();

            var index = trNode.SelectNodes("./td").IndexOf(tdNode);

            if (trNode.NextSibling == null)
            {
                trNode = trNode.SelectSingleNode("./tr");
            }
            else
            {
                trNode = trNode.NextSibling;

                while (trNode.Name.ToLower() != "tr")
                {
                    trNode = trNode.NextSibling;
                }
            }

            var Nodes = trNode.SelectNodes("./td");
            if (Nodes.Count <= index)
            {
                return "";
            }
            tdNode = Nodes[index];

            var value = WebQuery.Clean(tdNode.InnerText);

            return Regex.Replace(value, @"\s+", " ");
        }

        public override bool CanScrape(string county)
        {
            throw new NotImplementedException();
        }

        public override Item Scrape(string parcelNumber)
        {
            if (Delay > 0)
            {
                Thread.Sleep(Delay);
            }

            var doc = Search(parcelNumber);



            if (NoRecordsFound(doc))
            {
                InvokeNoItemsFound();
                return null;
            }

            //string link = GetLink(doc);
            //InvokeOpeningUrl();
            Item item = null;
            try
            {
                item = GetItem(doc);
                if (item.MapNumber == "n/a")
                {
                    item.MapNumber = parcelNumber;
                }
            }
            catch (Exception ex)
            {
                LogThrowErrorInField(doc, ex);
            }
            return item;
        }

        private Item GetItem(HtmlDocument doc)
        {
            //var doc = _webQuery.GetSource(link, 1);
            Logger.Log(doc.DocumentNode.InnerHtml);

            var item = new Item
            {
                MapNumber = GetValueOfField(doc, "Parcel Number"),
                LegalDescription = GetValueOfField(doc, "Legal Description"),
                Acreage = GetValueOfField(doc, "Acres"),
                PhysicalAddress1 = Utils.CleanMultipleSpaces(GetValueOfField(doc, "Location Address"))
            };
            string name = GetOwnerName(doc);
            AssignNames(item, name);

            AssignOwnerAddress(item, doc);

            item.MarketValue = GetValueValueOfField(doc, "Current Value");
            if (item.MarketValue == "n/a")
                item.MarketValue = GetValueValueOfField(doc, "Market Value");
   

            item.LandValue = GetValueValueOfField(doc, "Land Value");
            item.AccessoryValue = GetValueValueOfField(doc, "Accessory Value");

            var improvementValue = GetValueValueOfField(doc, "Improvement Value");

            if (string.IsNullOrWhiteSpace(improvementValue) || improvementValue == "n/a")
            {
                improvementValue = GetValueValueOfField(doc, "Building Value");
            }

            item.ImprovementValue = improvementValue;

            //var str = GetVerticalValueFromQpublic(doc, "Sq Ft", "Square Feet");
            var str = GetValueOfFieldWithFailover(doc, "Heated Square Feet", "Basement Square Feet");

            item.OwnerResident = GetOwnerResident(item);

            AssignAdditionalNote(doc, item, str, "sqft");
            str = GetValueOfFieldWithFailover(doc, "Number Of Bedrooms");
            AssignAdditionalNote(doc, item, str, "R/BR/BA/ExP");
            var eyearb = GetValueOfFieldWithFailover(doc, "Year Built");
            if (!string.IsNullOrWhiteSpace(eyearb) && eyearb != "n/a" && eyearb != "0")
            {
                AssignAdditionalNote(doc, item, eyearb, "YR");
            }

            str = GetValueOfFieldWithFailover(doc, "Condition");
            AssignAdditionalNote(doc, item, str, "condition");

            var accessory = doc.DocumentNode.SelectSingleNode("//section/header/span[contains(text(), 'Accessory Information')]");
            if (accessory != null)
            {
                var rows = accessory.ParentNode.NextSibling.NextSibling.SelectNodes("*//table/tbody/tr");

                if (rows != null && rows.Count > 0) //&& !(rows.Count == 3 && rows.Any(r => r.InnerHtml.Contains("No accessory information")))
                {
                    for (Int32 i = 0; i < rows.Count; ++i)
                    {
                        str = String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine;
                        var cells = rows[i].SelectNodes("td/text()");
                        if (cells != null)
                        {
                            for (Int32 j = 0; j < cells.Count; ++j)
                            {
                                //if (j == cells.Count - 2)
                                //{
                                //    continue;
                                //}

                                if (j != 0)
                                {
                                    str += ", ";
                                }

                                str += WebQuery.Clean(cells[j].InnerText).Replace("  ", "");
                            }
                        }
                        AssignAdditionalNote(doc, item, str, (String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine) + "Accessory " + (i + 1).ToString());
                    }
                }
            }

            var sales = doc.DocumentNode.SelectSingleNode("//section/header/span[contains(text(), 'Sales')]");
            if (sales != null)
            {
                var rows = sales.ParentNode.NextSibling.NextSibling.SelectNodes("*//table/tbody/tr");
                if (rows != null && rows.Count > 0) // && !(rows.Count == 3 && rows.Any(r => r.InnerHtml.Contains("No sales information"))))
                {
                    //var cols = rows[1].SelectNodes("td").Select(r => WebQuery.Clean(r.InnerText)).ToList();
                    //cols[1] = "DB";
                    //if (cols != null)
                    //{
                    for (Int32 i = 0; i < rows.Count; ++i)
                    {
                        str = String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine;
                        var cells = rows[i].SelectNodes("td");
                        if (cells != null)
                        {
                            for (Int32 j = 0; j < cells.Count; ++j)
                            {
                                //if (j == 2)
                                //{
                                //    continue;
                                //}

                                if (j != 0)
                                {
                                    str += ", ";
                                }

                                //if (j != 0 && j != 3)
                                //{
                                //    str += cols[j] + ":";
                                //}

                                //var tmpstr = WebQuery.Clean(cells[j].InnerText);
                                //if (j == 3)
                                //{
                                //    tmpstr = tmpstr.Replace(" ", "");
                                //}

                                //str += tmpstr;

                                str += WebQuery.Clean(cells[j].InnerText).Replace("  ", "");
                            }
                        }
                        AssignAdditionalNote(doc, item, str, (String.IsNullOrEmpty(item.Notes) ? "" : Environment.NewLine) + "Sale " + (i + 1).ToString());
                        //}
                    }

                }

            }
            item.Images = GetImages(doc);
            return item;
        }

        public HtmlNode GetOwnerNameNode(HtmlDocument doc)
        {
            return (doc.DocumentNode.SelectNodes("//span[contains(@id, 'lnkOwnerName')]") ?? doc.DocumentNode.SelectNodes("//a[contains(@id, 'lnkOwnerName')]") ?? doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblPrimaryOwnerName')]") ?? doc.DocumentNode.SelectNodes("//a[contains(@id, 'lblOwnerName_lnkSearch')]") ?? doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblOwnerName_lblSearch')]"))?.FirstOrDefault();
        }

        public HtmlNode GetOwnerNameAndAddressNode(HtmlDocument doc)
        {
            return doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblOwnerAddress')]")?.FirstOrDefault() ?? null;
        }

        public HtmlNode GetOwnerAddressNode(HtmlDocument doc)
        {
            return doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblAddress')]")?.FirstOrDefault() ?? null;
        }

        public HtmlNode GetOwnerStateCityZipNode(HtmlDocument doc)
        {
            return (doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblCityStateZip')]") ?? doc.DocumentNode.SelectNodes("//span[contains(@id, 'lblCityStZip')]"))?.FirstOrDefault() ?? null;
        }

        public override string GetOwnerName(HtmlDocument doc)
        {
            var node = GetOwnerNameNode(doc);

            var nodeText = string.Empty;

            if (node == null || string.IsNullOrWhiteSpace(node.InnerText.Trim()))
            {
                node = GetOwnerNameAndAddressNode(doc);
                if (node != null)
                {
                    var items = node.InnerHtml.Split(new[] { "<br>", "<br/>" }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Replace("\r\n", string.Empty)).ToList();
                    if (items != null && items.Count > 0)
                    {
                        nodeText = items[0];
                    }
                }
            }
            else
            {
                nodeText = node.InnerText;
            }

            return WebQuery.Clean(nodeText);
        }

        public string GetOwnerResident(Item item)
        {
            if (string.IsNullOrWhiteSpace(item.OwnerAddress) || string.IsNullOrWhiteSpace(item.PhysicalAddress1))
            {
                return "N";
            }

            if ((!string.IsNullOrWhiteSpace(item.OwnerAddress) && item.OwnerAddress.Length < 5) || (!string.IsNullOrWhiteSpace(item.PhysicalAddress1) && item.PhysicalAddress1.Length < 5))
            {
                return "N";
            }

            return item.OwnerAddress.Substring(0, 5) == item.PhysicalAddress1.Substring(0, 5) ? "Y" : "N";
        }

        private void AssignAdditionalNote(HtmlDocument doc, Item item, String value, String Abbr, Func<String, Boolean> FuncYesNo = null)
        {
            if (String.IsNullOrEmpty(value) || value == "n/a")
            {
                return;
            }

            if (FuncYesNo != null)
            {
                value = FuncYesNo(value) ? "Y" : "N";
            }

            item.Notes += String.Format("    {0}: {1}", Abbr, value);
        }

        public override Scraper GetClone()
        {
            return new ScraperQpublicNew(CountyCode);
        }
    }
}