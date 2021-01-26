using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace CTALookup
{
    public class WebQuery
    {
        private const int Timeout = 360000;
        public bool AllowAutoRedirect = true;
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0";
        //public string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36";
        public string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        public string ContentType = "application/x-www-form-urlencoded";

        private CookieContainer _cookies = new CookieContainer();
        public WebProxy Proxy { get; set; }
        public int Delay { get; set; }

        public void ClearCookies()
        {
            _cookies = new CookieContainer();
        }

        public string GetPlainSource(string url, int retries, string referer = null, bool xmlHttpRequest = false)
        {
            using (var reader = new StreamReader(GetStream(url, retries, xmlHttpRequest: xmlHttpRequest, referer: referer)))
            {
                return reader.ReadToEnd();
            }
        }

        public HtmlDocument GetPost(string url, string parameters, int retries, string referer = null, bool xmlHttpRequest = false) {
            //using ( var reader = new StreamReader(GetStream(url, retries, true, parameters, referer, xmlHttpRequest))) {
            using (var reader = new StreamReader(GetStream(url, retries, true, parameters, referer, xmlHttpRequest)))
            {
                string code = reader.ReadToEnd();
                var doc = new HtmlDocument();
                
                doc.LoadHtml(code);
                return doc;
            }
        }

        public HtmlDocument GetMultipartPost(string url, IDictionary<string, string> parameters, string referer = null) {
            if (Delay > 0)
            {
                Thread.Sleep(Delay);
            }

            string dataBoundary = String.Format("----------{0:N}", Guid.NewGuid());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebProxy webProxy = new WebProxy("http://zproxy.lum-superproxy.io:22225", true)
            {
                Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a", "lumtest.com/myip.json"),
                UseDefaultCredentials = false
            };

            request.Proxy = webProxy;
            request.Proxy.Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a");

            request.Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a");

            request.Method = "POST";
            request.Timeout = Timeout;
            request.AllowAutoRedirect = AllowAutoRedirect;
            request.UserAgent =
                UserAgent;
            request.Accept = Accept;
            request.ContentType = ContentType;
            request.CookieContainer = _cookies;

            request.KeepAlive = false;
            request.ContentType = "multipart/form-data; boundary=" + dataBoundary;
            if (Proxy != null)
            {
                request.Proxy = Proxy;
            }
            if (referer != null)
            {
                request.Referer = referer;
            }

            byte[] body = GetMultipartFormData(parameters, dataBoundary);

            using (var str = request.GetRequestStream())
            {
                str.Write(body, 0, body.Length);
                str.Close();
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var stream = response.GetResponseStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(stream);
            return doc;
        }

        private static byte[] GetMultipartFormData(IDictionary<string, string> postParameters, string boundary)
        {
            Stream formDataStream = new MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetByteCount("\r\n"));

                needsCLRF = true;

                string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                boundary,
                param.Key,
                param.Value);
                formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetByteCount(postData));
            }

            // Add the end of the request. Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(Encoding.UTF8.GetBytes(footer), 0, Encoding.UTF8.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }

        public Stream Invoke(string url, int retries, bool post = false, string postParameters = null, string referer = null, bool xmlHttpRequest = false)
            //(string Method, string Uri, string Body)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            var proxy = new WebProxy
            {
                Address = new Uri($"http://{"zproxy.lum-superproxy.io"}:{"22225"}"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,

                // *** These creds are given to the proxy server, not the web server ***
                Credentials = new NetworkCredential(
                            userName: "lum-customer-hl_389267c6-zone-static",
                            password: "e7ecycprce6a")
            };
            // Now create a client handler which uses that proxy
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };

            // Omit this part if you don't need to authenticate with the web server:
            //if (needServerAuthentication)
            {
                httpClientHandler.PreAuthenticate = true;
                httpClientHandler.UseDefaultCredentials = false;
                httpClientHandler.AllowAutoRedirect = true;
                httpClientHandler.CookieContainer = _cookies;

                // *** These creds are given to the web server, not the proxy server ***
                httpClientHandler.Credentials = new NetworkCredential(
                    userName: "lum-customer-hl_389267c6-zone-static",
                    password: "e7ecycprce6a");
            }
            //httpClient = new HttpClient(handler: httpClientHandler, disposeHandler: true);
            //httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cl = new HttpClient(handler: httpClientHandler, disposeHandler: true);
            cl.BaseAddress = new Uri(url);
            cl.Timeout = TimeSpan.FromMilliseconds(Timeout);
            int _TimeoutSec = 90;
            cl.Timeout = new TimeSpan(0, 0, _TimeoutSec);
            //string _ContentType = "application/json";
            //cl.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_ContentType));

            //var _UserAgent = "d-fens HttpClient";
            // You can actually also set the User-Agent via a built-in property
            cl.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            // You get the following exception when trying to set the "Content-Type" header like this:
            // cl.DefaultRequestHeaders.Add("Content-Type", _ContentType);
            // "Misused header name. Make sure request headers are used with HttpRequestMessage, response headers with HttpResponseMessage, and content headers with HttpContent objects."
            HttpResponseMessage response;
            try
            {
               
            string Method = "Get";
            if (post)
                Method = "POST";
            var _Method = new HttpMethod(Method);
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            {
                
                if (true) return true;
                
            };

           
                switch (_Method.ToString().ToUpper())
                {
                    case "GET":
                    case "HEAD":
                        Console.WriteLine(cl.ToString());
                        // synchronous request without the need for .ContinueWith() or await
                        response = cl.GetAsync(url).Result;
                        break;
                    case "POST":
                        {
                            // Construct an HttpContent from a StringContent
                            HttpContent _Body = new StringContent(postParameters);
                            // and add the header to this object instance
                            // optional: add a formatter option to it as well
                            _Body.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
                            // synchronous request without the need for .ContinueWith() or await
                            response = cl.PostAsync(url, _Body).Result;
                        }
                        break;
                    case "PUT":
                        {
                            // Construct an HttpContent from a StringContent
                            HttpContent _Body = new StringContent(postParameters);
                            // and add the header to this object instance
                            // optional: add a formatter option to it as well
                            _Body.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
                            // synchronous request without the need for .ContinueWith() or await
                            response = cl.PutAsync(url, _Body).Result;
                        }
                        break;
                    case "DELETE":
                        response = cl.DeleteAsync(url).Result;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            
            // either this - or check the status to retrieve more information
            response.EnsureSuccessStatusCode();
            // get the rest/content of the response in a synchronous way
            var content = response.Content.ReadAsStringAsync().Result;

            return response.Content.ReadAsStreamAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        private Stream GetStream(string url, int retries, bool post = false, string postParameters = null, string referer = null, bool xmlHttpRequest = false) {
            for (int i = 0; i < retries; i++)
            {
                if (Delay > 0) {
                    Thread.Sleep(Delay);
                }
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                WebProxy webProxy = new WebProxy("http://zproxy.lum-superproxy.io:22225", true)
                {
                    Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a", "lumtest.com/myip.json"),
                    UseDefaultCredentials = false
                };

                request.Proxy = webProxy;
                request.Proxy.Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a");

                request.Credentials = new NetworkCredential("lum-customer-hl_389267c6-zone-static", "e7ecycprce6a");
                request.Method = post ? "POST" : "GET";
                request.Timeout = Timeout;
                request.AllowAutoRedirect = AllowAutoRedirect;
                request.UserAgent =
                    UserAgent;
                request.Accept = Accept;
                request.ContentType = ContentType;
                request.CookieContainer = _cookies;
                request.KeepAlive = false;
                /*request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)";*/
                if (xmlHttpRequest)
                {
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    request.Headers.Add("X-Prototype-Version", "1.7");
                    request.Headers.Add("X-MicrosoftAjax", "Delta=true");
                    
                }
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                }
                if (referer != null)
                {
                    request.Referer = referer;
                }
                System.Net.ServicePointManager.Expect100Continue = false;
                if (post) {
                    using (StreamWriter sw = new StreamWriter(request.GetRequestStream()))
                    {
                        sw.Write(postParameters);
                    }
                }

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    return response.GetResponseStream();
                }
                catch (Exception ex)
                {
                    if (i != retries - 1)
                    {
                        continue;
                    }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }
            throw new Exception("?");
        }
        public HtmlDocument GetSource(string url, int retries, string referer = null)
        {
            Logger.Log($"Scrape url:{url}");
            var doc = new HtmlDocument();
            doc.LoadHtml(GetPlainSource(url, retries, referer));
            return doc;
        }

        public Image GetImage(string url, int retries)
        {
            return Image.FromStream(GetStream(url, retries));
        }

        public static string Clean(string text)
        {
            return HttpUtility.HtmlDecode(text).Trim();
        }
        
        public static string BuildUrl(string url, string baseUrl) {
            return url.StartsWith("http") ? url : baseUrl + url;
        }
        
        /// <summary>
        /// Method that returns the post parameters from a dictionary containing the names as Keys and values as Values.
        /// </summary>
        /// <example>user=john&pass=john00&function=check</example>
        /// <param name="postParameters">The dictionary containing the post parameters</param>
        /// <returns>The string that represents the post parameters</returns>
        public static string GetStringFromParameters(Dictionary<string, string> postParameters, bool encode = true)
        {
            string result = "";
            if (postParameters == null)
            {
                throw new ArgumentNullException("postParameters");
            }

            foreach (var pair in postParameters)
            {
                if (encode)
                {
                    result += String.Format("{0}={1}{2}", pair.Key, HttpUtility.UrlEncode(pair.Value), "&");
                }
                else {
                    result += String.Format("{0}={1}{2}", pair.Key, pair.Value, "&");
                }
            }
            return result.Length == 0 ? result : result.Substring(0, result.Length - 1);
        }
    }
}