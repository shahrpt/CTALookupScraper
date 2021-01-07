using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CTALookup {
    public class Utils {
        public const string RegexCityStateZip = @"(.*)\s+(.*)\s+(.*)";

        private static void GetNamesAfterCO(ref string fullName, ref string firstName, ref string middleInitial,
            ref string lastName) {
            string[] splitted = Regex.Split(fullName, "c/o", RegexOptions.IgnoreCase);
            if (splitted.Length != 2) {
                firstName = "n/a";
                middleInitial = "n/a";
                lastName = "n/a";
                return;
            }

            string name = splitted[1].Trim();
            //
            if (TextContainsCompanyWords(name)) {
                lastName = name;
                return;
            }


            // In order to set the C/O removed name to owner name. but disabled
            //string split0 = splitted[0].Trim();
            //if (!string.IsNullOrEmpty(split0))
            //    fullName = split0;


            string fNameTemp = string.Empty;    // Dummy variable to keep fullName the Raw Data
            Utils.GetNames(name, ref fNameTemp, ref firstName, ref middleInitial, ref lastName, false);
            if ((firstName.Length < 2 || lastName.Length < 2) && !(firstName.Length == 0 && lastName.Length > 2))
            {
                Utils.GetNames(name, ref fNameTemp, ref firstName, ref middleInitial, ref lastName, true);
                if (firstName.Length < 2 || lastName.Length < 2)
                    Utils.GetNames(name, ref fNameTemp, ref firstName, ref middleInitial, ref lastName, false);
            }


            /*
            splitted = name.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            firstName = splitted[0];
            lastName = splitted[splitted.Length - 1];



            if (splitted.Length > 2) {
                middleInitial = splitted[1][0].ToString();

                //The middle initial must be a letter
                CheckMiddleInitial(ref middleInitial);
            }*/
        }

        public static void RemoveDecimalValues(Item item) {
            if (!string.IsNullOrEmpty(item.MarketValue)) {
                item.MarketValue = RemoveDecimal(item.MarketValue);
            }
            if (!string.IsNullOrEmpty(item.ImprovementValue)) {
                item.ImprovementValue = RemoveDecimal(item.ImprovementValue);
            }
            if (!string.IsNullOrEmpty(item.LandValue)) {
                item.LandValue = RemoveDecimal(item.LandValue);
            }
        }

        private static string RemoveDecimal(string text) {
            return Regex.Replace(text, @"\.\d+", "");
        }

        private static void GetNamesWithDuplicateLastnames(ref string fullName, ref string firstName,
            ref string middleInitial, ref string lastName) {
            string[] splitted = fullName.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            splitted = splitted.Select(x => x.TrimEnd(',', ' ')).ToArray();

            string last = splitted[0].ToLower();

            int index = 1;
            for (int i = 1; i < splitted.Length; i++) {
                if (splitted[i].ToLower() == last) {
                    index = i;
                    break;
                }
            }

            splitted = splitted.Take(index).ToArray();
            string text = String.Join(" ", splitted);
            ParseName(text, out firstName, out middleInitial, out lastName);
        }

        public static void GetNames(string text, ref string fullName, ref string firstName, ref string middleInitial,
            ref string lastName, bool firstMiddleLast = false) {
            fullName = text;

            if (fullName.ToLower().Contains("c/o")) {
                GetNamesAfterCO(ref fullName, ref firstName, ref middleInitial, ref lastName);
                return;
            }

            if (TextContainsCompanyWords(text)) {
                lastName = text;
                return;
            }

            //Check if the lastname appears multiple times
            string[] splitted = fullName.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (splitted.Select(x => x.ToLower()).Count(x => x == splitted[0].ToLower()) > 1) {
                GetNamesWithDuplicateLastnames(ref fullName, ref firstName, ref middleInitial, ref lastName);
                return;
            }

            text = RemoveAnd(text);
            text = RemoveAka(text);
            text = RemoveJrSr(text);
            text = RemoveDba(text);
            text = RemoveEtAl(text);
            text = RemoveLifeState(text);
            text = RemoveIIandIII(text);
            text = RemoveMrMrsMissMs(text);
            if (!IsName(text)) {
                lastName = text;
                return;
            }

            //Keep only what is before the first &
            text = text.Split('&')[0].Trim();

            //Remove the /

            //text = Regex.Replace(text, @"/[^\s]+", "");
            var ind = text.IndexOf("/");
            if (ind > -1) text = text.Substring(0, ind);

            //If it is a name
            ParseName(text, out firstName, out middleInitial, out lastName, firstMiddleLast);
        }

        private static void ParseName(string text, out string firstName, out string middleInitial, out string lastName, bool firstMiddleLast = false) {
            string[] splitted = text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            if (splitted.Length == 0)
            {
                firstName = "N/A";
                lastName = "N/A";
                middleInitial = "N/A";
                return;
            }
                

            middleInitial = "";
            lastName = "";
            //if (splitted.Length == 0) return;
            if (splitted.Length == 1) {
                //Assign it to Name or Lastname?
                firstName = splitted[0];
            }
            else if (splitted.Length == 2) {
                if (firstMiddleLast) {
                    firstName = splitted[0];
                    lastName = splitted[1];
                }
                else {
                    firstName = splitted[1];
                    lastName = splitted[0];
                }
            }
            else 
            {
                if (firstMiddleLast)
                {
                    firstName = splitted[0];
                    middleInitial = splitted[1][0].ToString();
                    lastName = splitted[2];
                }
                else
                {
                    firstName = splitted[1];//splitted[1].Length == 1 ? splitted[2] : splitted[1];
                    lastName = splitted[0];
                    middleInitial = splitted[2][0].ToString();//splitted[1].Length == 1 ? splitted[1] : splitted[2][0].ToString();
                }

                //The middle initial must be a letter
                CheckMiddleInitial(ref middleInitial);
            }
        }

        private static void CheckMiddleInitial(ref string middleInitial) {
            if (!char.IsLetter(middleInitial[0])) {
                middleInitial = "";
            }
        }

        private static string RemoveAnd(string text) {
            return Regex.Replace(text, @"\s+and(\s|$).*", "", RegexOptions.IgnoreCase);
        }

        private static string RemoveAka(string text) {
            text = Regex.Replace(text, @"\s+aka(\s|$).*", "", RegexOptions.IgnoreCase);
            return Regex.Replace(text, @"\s+a/k/a(\s|$).*", "", RegexOptions.IgnoreCase);
        }

        public static void GetCityStateZip(string text, ref string city, ref string state, ref string zip,
            string regex = null) {
            var match = Regex.Match(text, regex ?? RegexCityStateZip);
            if (!match.Success) {
                city = text;
                return;
                //throw new Exception(string.Format("Error getting city, state and address from \"{0}\"", text));
            }
            city = match.Groups[1].Value;
            state = match.Groups[2].Value;
            zip = match.Groups[3].Value;
            GetAndCleanCityStateZip(ref city, ref state, ref zip);
        }

        public static void GetAddress(string text, ref string address, ref string city, ref string state, ref string zip) {
            var match = Regex.Match(text, string.Format(@"(.*), {0}", RegexCityStateZip));
            if (!match.Success) {
                throw new Exception(string.Format(@"Error getting entire address from ""{0}""", text));
            }
            address = match.Groups[1].Value;
            city = match.Groups[2].Value;
            state = match.Groups[3].Value;
            zip = match.Groups[4].Value;
            GetAndCleanCityStateZip(ref city, ref state, ref zip);
        }

        private static void GetAndCleanCityStateZip(ref string city, ref string state, ref string zip) {
            city = city.Trim();
            state = state.Trim();
            zip = zip.Trim();

            if (city.EndsWith("-")) {
                city = city.Substring(0, city.Length - 1);
            }
            if (state.EndsWith(",")) {
                state = state.Substring(0, state.Length - 1);
            }
            if (zip.EndsWith("-")) {
                zip = zip.Substring(0, zip.Length - 1);
            }
        }

        private static string RemoveDba(string text) {
            return RemoveRegex(text, @"\s+dba(\s|$).*");
        }

        private static string RemoveJrSr(string text) {
            return RemoveRegex(text, @"\s+(\()?(j|s)r(\))?(\s|$|/).*");
        }

        private static string RemoveMrMrsMissMs(string text) {
            return RemoveRegex(text, @"\s+m(r|rs|iss|s)(\s|$).*");
        }

        private static string RemoveIIandIII(string text) {
            text = RemoveRegex(text, @"\s+II(\s|$).*");
            return RemoveRegex(text, @"\s+III(\s|$).*");
        }

        private static string RemoveLifeState(string text) {
            return RemoveRegex(text, @"(\()?life est(ate)?(\))?");
        }

        private static string RemoveRegex(string text, string regex) {
            text = Regex.Replace(text, regex, "", RegexOptions.IgnoreCase);
            return text.Trim();
        }

        private static string RemoveEtAl(string text) {
            return RemoveRegex(text, @"et al\s*");
        }

        private static bool IsName(string text) {
            string[] splittedByAnd = text.Split('&').Select(x => x.Trim()).ToArray();
            if (splittedByAnd.Length > 1) {
                if (splittedByAnd[0].Length == 1) {
                    return false;
                }
            }

            if (TextContainsCompanyWords(text)) {
                return false;
            }

            return true;
        }

        public static bool TextContainsCompanyWords(string text) {
            string[] companyWords =
                {
                    "administrations?",
                    "americas?",
                    "americans?",
                    "assets?",
                    "assocs?",
                    "associates",
                    "associations?",
                    "bank",
                    "bankings?",
                    "bldrs?",
                    "LP",
                    "builderss?",
                    "businesss?",
                    "cares?",
                    "centrals?",
                    "cos?",
                    "columbias?",
                    "communitys?",
                    "communitys?",
                    "companys?",
                    "constructions?",
                    "consultantss?",
                    "developerss?",
                    "developments?",
                    "diversionss?",
                    "enterprises",
                    "entertainments?",
                    "equitys?",
                    "familys?",
                    "finances?",
                    "financials?",
                    "funds?",
                    "fundings?",
                    "globals?",
                    "groups?",
                    "healths?",
                    "hoas?",
                    "holdings?",
                    "homeownerss?",
                    "homess?",
                    "housings?",
                    "incs?",
                    "incomes?",
                    "incorporates?",
                    "incorporateds?",
                    "industriess?",
                    "internationals?",
                    "investorss?",
                    "land holdings?",
                    "land holdingss?",
                    "landings?",
                    "limiteds?",
                    "livings?",
                    "llc?",
                    "loans?",
                    "ltd?",
                    "managements?",
                    "ministriess?",
                    "mortgages?",
                    "partnerss?",
                    "partnerships?",
                    "pointes?",
                    "properties",
                    "property",
                    "ptshps?",
                    "realestates?",
                    "realtys?",
                    "realtys?",
                    "revocables?",
                    "sales?",
                    "savings?",
                    "solutions?",
                    "southerns?",
                    "states?",
                    "strategic",
                    "strategies",
                    "strategy",
                    "supply",
                    "trusts?",
                    //"trustee",
                    "urbans?",
                    "utilities?",
                    "ventures?",
                    "warehouse",
                    "associates?",
                    "llc",
                    "association",
                    "assoc",
                    "company",
                    @"co\.",
                    "corp",
                    "inc",
                    "incorporated",
                    "incorp",
                    "limited liability",
                    "enterprise",
                    "partnership",
                    "society",
                    "syndicate",
                    "trust",
                    "partners",
                    "corp",
                    "in trust",
                    "care of",
                    //"trustee",
                    "proprietorship",
                    "community",
                    "development",
                    "foundation",
                    "church",
                    "temple",
                    "synagogue",
                    "investments?",
                    "worship",
                    "center",
                    @"L\s*L\s*C", //L L C
                    "L P",
                    @"real\s+estate", //real estate
                    "incorp",
                    "corporate",
                    "corporation",
                    @"L\.L\.C", //L.L.C
                    "ptnrshp",
                    "resort",
                    "LLP",
                    "securities",
                    "security",
                    "PLLC",
                    "commons",
                    "clinical",
                    "laboratory",
                    "custodian",
                    "c-corp",
                };
            var lower = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);

            foreach (var word in companyWords) {
                if (Regex.Match(lower, string.Format(@"\b{0}\b", word), RegexOptions.IgnoreCase).Success) {
                    return true;
                }
            }
            return false;
        }

        public static Boolean CheckAvoidWords(ref string text, Boolean remove = true)
        {
            string[] Words =
                {
                    "trustee",
                    "tr",
                    "attn:"
                };
            var lower = text.ToLower();
            var Lparts = lower.Split(' ').ToList();
            var parts= text.Split(' ').ToList();
            var res = false;
            foreach (var word in Words)
            {
                //var ind = lower.IndexOf(word);
                var ind = Lparts.IndexOf(word);
                if (ind == -1) continue;
                res = true;
                if (!remove) break;
                //text = text.Substring(0, ind) + text.Substring(ind+word.Length);
                parts.RemoveAt(ind);
                Lparts.RemoveAt(ind);
            }
            //text = text.Trim();
            if (remove) text = String.Join(" ", parts);
            return res;
        }

        public static void AssignFullAddrToPhysicalAddress(Item item, string text, string addrSeparator = ",",
            string regex = "(?<city>.+) (?<zip>.+)") {
            string addr = "";
            string city = "";
            string state = "";
            string zip = "";

            GetFullAddress(text, ref addr, ref city, ref state, ref zip, addrSeparator, regex);

            item.PhysicalAddress1 = addr;
            item.PhysicalAddressCity = city;
            item.PhysicalAddressState = state;
            item.PhysicalAddressZip = zip;
        }

        public static void AssignFullAddrToOwnerAddress(Item item, string text, string addrSeparator = ",",
            string regex = "(?<city>.+) (?<zip>.+)") {
            string addr = "";
            string city = "";
            string state = "";
            string zip = "";

            GetFullAddress(text, ref addr, ref city, ref state, ref zip, addrSeparator, regex);

            item.OwnerAddress = addr;
            item.OwnerCity = city;
            item.OwnerState = state;
            item.OwnerZip = zip;
        }

        private static void GetFullAddress(string fullAddress, ref string address, ref string city, ref string state,
            ref string zip, string addrSeparator, string regex) {
            fullAddress = Regex.Replace(fullAddress, @"\s+", " ").Replace(" ,", ",");

            if (string.IsNullOrEmpty(fullAddress)) {
                return;
            }

            //If the address contains the city and zip
            if (fullAddress.Contains(addrSeparator) &&
                Char.IsNumber(fullAddress[fullAddress.Length-1]))
            {
                string[] splitted = fullAddress.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);

                int count = regex.Count(x => x == ',') + 1;
                string text = "";
                for (int i = splitted.Length - 1; i >= splitted.Length - count; i--) {
                    text = splitted[i].Trim() + ", " + text;
                }

                text = text.Trim(',', ' ');

                address = fullAddress.Replace(text, "").Trim().TrimEnd(',', ' ');

                var m = Regex.Match(text, regex);
                try {
                    city = m.Groups["city"].Value;
                }
                catch {}
                try {
                    state = m.Groups["state"].Value;
                }
                catch {}
                try {
                    zip = m.Groups["zip"].Value;
                }
                catch {}
            }
            else {
                address = fullAddress;
            }
        }

        public static string CleanMultipleSpaces(string input)
        {
            return Regex.Replace(input, @"\s+", " ");
        }

        private static string[] _abbreviations = new[]
            {
                "ALY",
                "ANX",
                "ARC",
                "AVE",
                "BCH",
                "BG	",
                "BGS",
                "BLF",
                "BLFS",
                "BLVD",
                "BND",
                "BR",
                "BRG",
                "BRK",
                "BRKS",
                "BTM",
                "BYP",
                "BYU",
                "CHSE",
                "CIR",
                "CIRS",
                "CLB",
                "CLF",
                "CLFS",
                "CMN",
                "CMNS",
                "COR",
                "CORS",
                "CP",
                "CPE",
                "CRES",
                "CRK",
                "CRSE",
                "CRST",
                "CSWY",
                "CT",
                "CTR",
                "CTRS",
                "CTS",
                "CURV",
                "CV",
                "CVS",
                "CYN",
                "DL",
                "DM",
                "DR",
                "DRS",
                "DV",
                "EST",
                "ESTS",
                "EXPY",
                "FALL",
                "FLD",
                "FLDS",
                "FLS",
                "FLT",
                "FLTS",
                "FORT",
                "FRD",
                "FRDS",
                "FRG",
                "FRGS",
                "FRK",
                "FRKS",
                "FRST",
                "FRY",
                "FWY",
                "GDN",
                "GDNS",
                "GLN",
                "GLNS",
                "GRN",
                "GRNS",
                "GRV",
                "GRVS",
                "GTWY",
                "HBR",
                "HBRS",
                "HL",
                "HLS",
                "HOLW",
                "HTS",
                "HVN",
                "HWY",
                "INLT",
                "IS",
                "ISLE",
                "ISS",
                "JCT",
                "JCTS",
                "KNL",
                "KNLS",
                "KY",
                "KYS",
                "LAND",
                "LCK",
                "LCKS",
                "LDG",
                "LGT",
                "LGTS",
                "LK",
                "LKS",
                "LN",
                "LNDG",
                "LOOP",
                "MALL",
                "MDW",
                "MDWS",
                "MEWS",
                "ML",
                "MLS",
                "MNR",
                "MNRS",
                "MSN",
                "MT",
                "MTN",
                "MTNS",
                "MTWY",
                "NCK",
                "OPAS",
                "ORCH",
                "OVAL",
                "OVLK",
                "PARK",
                "PARKS",
                "PASS",
                "PATH",
                "PIKE",
                "PKWY",
                "PL",
                "PLN",
                "PLNS",
                "PLZ",
                "PNE",
                "PNES",
                "POND",
                "PR",
                "PRT",
                "PRTS",
                "PSGE",
                "PT",
                "PTE",
                "PTS",
                "RADL",
                "RAMP",
                "RD",
                "RDG",
                "RDGS",
                "RDS",
                "RIV",
                "RNCH",
                "ROW",
                "RPD",
                "RPDS",
                "RST",
                "RUE",
                "RUN",
                "SHL",
                "SHLS",
                "SHR",
                "SHRS",
                "SKWY",
                "SMT",
                "SPG",
                "SPGS",
                "SPUR",
                "SPURS",
                "SQ",
                "SQS",
                "ST",
                "STA",
                "STRA",
                "STRM",
                "STS",
                "TER",
                "TPKE",
                "TRAK",
                "TRCE",
                "TRFY",
                "TRL",
                "TRLR",
                "TRWY",
                "TUNL",
                "UN",
                "UNS",
                "UPAS",
                "VIA",
                "VIS",
                "VL",
                "VLG",
                "VLGS",
                "VLY",
                "VLYS",
                "VW",
                "VWS",
                "WALK",
                "WALKS",
                "WALL",
                "WAY",
                "WAYS",
                "WHF",
                "WL",
                "WLS",
                "XING",
                "XRD",
                "XRDS"
            };

        private static string[] _suitesToRemove = {"SUITE", "STE", "NO", "APT", "#", "UNIT", "PMB", "LOT"};

        internal static System.Collections.Generic.List<String> _streetDirections = new System.Collections.Generic.List<String>(){ "E", "N", "NE", "NW", "S", "SE", "SW", "W"};
        /// <summary>
        /// Splits the specified address into Address 1 and Address 2.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string[] SplitAddress(string address) {
            string[] result = {address, ""};

            address = address.ToUpper();

            var parts = address.Split(' ').ToList();
            parts.RemoveAll(x => String.IsNullOrEmpty(x.Trim()));
            var abbrs = _abbreviations.Where(x => parts.Contains(x)).ToList();
            if (abbrs != null && abbrs.Count > 0)
            {
                var ind = abbrs.Select(x => parts.IndexOf(x)).Max();
                if (ind + 1 == parts.Count) return result;

                if (_streetDirections.Contains(parts[ind + 1].Trim(',','.'))) ++ind;
                else if (_streetDirections.Contains(parts[ind - 1].Trim(',','.'))) return result;

                if (ind + 1 != parts.Count)
                {
                    result[0] = parts.Take(ind + 1).Aggregate((x, y) => x + " " + y);
                    result[1] = parts.Skip(ind + 1).Aggregate((x, y) => x + " " + y);
                }
            }
            /*foreach (var abbr in _abbreviations) {
                var pattern = @" " + abbr + @" ";
                var m = Regex.Match(address, pattern, RegexOptions.IgnoreCase);

                if (m.Success) {

                    string[] splitted = Regex.Split(address, pattern);
                    result[0] =
                        string.Join(pattern, splitted.Select(x => x.Trim()).Take(splitted.Length - 1).ToArray()) +
                        pattern.TrimEnd();
                    result[1] = splitted[splitted.Length - 1].Trim(' ', '-');
                    break;
                }
            }*/

            if (!string.IsNullOrEmpty(result[1])) {
                foreach (var s in _suitesToRemove) {
                    if (result[1].StartsWith(s)) {
                        result[1] = "STE " + result[1].Substring(s.Length, result[1].Length - s.Length).Trim();
                        break;
                    }
                }
            }

            return result;
        }
    }
}