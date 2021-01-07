using System;
using System.Linq;
using Newtonsoft.Json;
using Velyo.Google.Services;

namespace CTALookup.GoogleMaps
{
    public class AddressScraper : IAddressScraper
    {
        public Address Scrape(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }
            var request = new GeocodingRequest(keyword);
            var response = request.GetResponse();
            Logger.Log(String.Format("Address request:{0}\nAddress response:{1}", JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(response)));
            if (response?.Results == null || response.Results.Count <= 0)
            {
                return null;
            }

            var addrItems = response.Results[0].FormattedAddress.Split(new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);

            var address = new Address
            {
                City =
                    response.Results[0].AddressComponents.FirstOrDefault(
                        x => x.Types.Contains("locality") && x.Types.Contains("political"))?.LongName,
                Zip =
                    response.Results[0].AddressComponents.FirstOrDefault(x => x.Types.Contains("postal_code"))?.LongName,
                State =
                    response.Results[0].AddressComponents.FirstOrDefault(
                            x => x.Types.Contains("administrative_area_level_1") && x.Types.Contains("political"))?
                        .ShortName,
                County =
                    response.Results[0].AddressComponents.FirstOrDefault(
                        x => x.Types.Contains("administrative_area_level_2") && x.Types.Contains("political"))?.LongName,
                Street = addrItems.Length > 0 ? addrItems[0] : ""
            };

            if (address.City == null)
            {
                address.City =
                    response.Results[0].AddressComponents.FirstOrDefault(x => x.Types.Contains("political"))?.LongName;
            }

            return address;
        }
    }
}