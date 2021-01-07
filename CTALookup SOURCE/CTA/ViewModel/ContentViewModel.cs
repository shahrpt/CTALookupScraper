using CTALookup.Georgia;
using CTALookup.GoogleMaps;
using CTALookup.Maryland;
using CTALookup.Scrapers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTALookup.ViewModel
{
    public class ContentViewModel : ViewModelBase
    {
        private Dictionary<string, string> _georgiaCounties;
        private Dictionary<string, string> _marylandCounties;
        private static IList<string> _arizonaCounties;
        private static IList<string> _southCarolinaCounties;
        private bool _stop;
        private bool _imagesSaved;

        public static Dictionary<string, string> GeorgiaCounties
        {
            get { return GetGeorgiaCounties(); }
        }

        public ContentViewModel()
        {
            Total = 100;
            ThreadsTotal = 1;
            RecordsPerFile = 0;
            PopulateStatesAndCounties();
            //_googleAddrScraper = new AddressScraper();
            //_addressScraper = new ZillowScraper();
            _addressScraper = new AddressScraper();
            _scraperFactory = new ScraperFactory();

            if (IsInDesignMode)
            {
                Current = 30;
                IsWorking = true;
                CurrentParcelText = "Current Parcel: 123-456-789A";
                StatusCountText = "Parcel 3 of 10";
                GetParcelSamplesVisible = true;
            }

            AddScrapersToFactory();

            ImagesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "property tax lien photos");
            PhotosSuffix = "0a";

            LastExportPath = ConfigurationManager.AppSettings["LastExportPath"];
        }

        private void AddScrapersToFactory()
        {
            //Counties that has '_dw' in the url
            _dwCounties = new[]
                {
                    "ga_bartow",
                    "ga_cherokee",
                    "ga_dekalb",
                    "ga_forsyth",
                    "ga_fulton",
                    "ga_glynn",
                    "ga_habersham",
                    "ga_muscogee",
                    //"ga_paulding", no more
                    "ga_white"
                };

            //Add georgia scrapers
            foreach (var x in _georgiaCounties)
            {
                Scraper s;

                if (x.Value == "ga_whitfield")
                {
                    s = new ScraperWhitfield();
                }
                else if (x.Value == "ga_bartow")
                {
                    s = new ScraperBartow();
                }
                else if (x.Value == "ga_henry")
                {
                    s = new ScraperHenry();
                    /*string searchUrl =string.Format("http://qpublic7.qpublic.net/ga_henry_search.php?county={0}&search=parcel", x.Value);
                    string submitUrl = "http://qpublic7.qpublic.net/ga_henry_alsearch.php";
                    s = new ScraperGeorgia(x.Key, x.Value, searchUrl, submitUrl);*/
                }
                else if (x.Key == "Chatham")
                {
                    s = new ScraperChatham();
                }
                else if (x.Value == "ga_clayton")
                {
                    s = new ScraperClayton();
                }
                else if (x.Value == "ga_dekalb" || x.Value == "ga_forsyth" || x.Value == "ga_fulton")
                {
                    s = new ScraperGeorgia(x.Key, x.Value,
                        string.Format("http://qpublic9.qpublic.net/ga_search_dw.php?county={0}&search=parcel", x.Value),
                        "http://qpublic9.qpublic.net/ga_alsearch_dw.php",
                        baseUrl: "http://qpublic9.qpublic.net/");
                }
                else if (x.Value == "ga_glynn")
                {
                    s = new ScraperGeorgia(x.Key, x.Value,
                        "http://qpublic7.qpublic.net/ga_glynn_search.php?county=ga_glynn&search=parcel",
                        "http://qpublic7.qpublic.net/ga_glynn_alsearch.php");
                }
                else
                {
                    bool dwCounty = _dwCounties.Contains(x.Value);
                    string searchUrl =
                        string.Format("http://qpublic7.qpublic.net/ga_search{0}.php?county={1}&search=parcel",
                            dwCounty ? "_dw" : "",
                            x.Value);
                    string submitUrl = string.Format("http://qpublic7.qpublic.net/ga_alsearch{0}.php",
                        dwCounty ? "_dw" : "");

                    s = new ScraperGeorgia(x.Key, x.Value, searchUrl, submitUrl
                        );
                }

                _scraperFactory.Add(s);
            }
        }

        private string StateCounty
        {
            get
            {
                return string.Format("{0}:{1}", SelectedState, SelectedCounty);
            }
        }

        #region Id

        public const string IdPropertyName = "Id";

        private int _id = 0;

        public int Id
        {
            get { return _id; }

            set
            {
                if (_id == value)
                {
                    return;
                }

                _id = value;
                RaisePropertyChanged(IdPropertyName);
            }
        }

        #endregion

        #region Populate Methods

        private void PopulateStatesAndCounties()
        {
            PopulateGeorgiaCounties();
            PopulateMarylandCounties();
            PopulateArizonaCounties();
            PopulateSouthCarolinaCounties();
            PopulateStates();
        }

        private void PopulateSouthCarolinaCounties()
        {
            _southCarolinaCounties = new List<string>
                {
                    "Aiken",
                    "Anderson",
                    "Beaufort",
                    "Charleston",
//                        "Dorchester",
                    "Fairfield",
                    "Georgetown",
                    "Greenville",
                    "Greenwood",
                    "Horry",
                    "Kershaw",
                    "Laurens",
                    "Lexington",
                    "Oconee",
                    "Orangeburg",
                    "Pickens",
                    "Richland",
                    "Spartanburg",
                    "York"
                };
        }

        private void PopulateArizonaCounties()
        {
            _arizonaCounties = new List<string>
                {
                    "Apache",
                    "Coconino",
                    "Gila",
                    "Graham",
                    "La Paz",
                    "Maricopa",
                    "Mohave",
                    "Navajo",
                    "Pima",
                    "Pinal",
                    "Santa Cruz",
                    "Yavapai",
                    "Yuma",
                };
        }

        private static string GetStateCode(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return "";
            }

            switch (state.Trim().ToLower())
            {
                case "georgia":
                    return "GA";
                case "south carolina":
                    return "SC";
                case "maryland":
                    return "MD";
                case "arizona":
                    return "AZ";
            }

            return "";
        }

        private void PopulateStates()
        {
            States = new ObservableCollection<string>
                {
                    "South Carolina",
                    "Maryland",
                    "Georgia",
                    "Arizona"
                };
        }

        #endregion

        #region Items

        public const string ItemsPropertyName = "Items";

        private ObservableCollection<Item> _items = new ObservableCollection<Item>();

        public ObservableCollection<Item> Items
        {
            get { return _items; }

            set
            {
                if (_items == value)
                {
                    return;
                }

                _items = value;
                RaisePropertyChanged(ItemsPropertyName);
            }
        }

        #endregion

        #region IsWorking

        public const string IsWorkingPropertyName = "IsWorking";

        private bool _isWorking;

        public bool IsWorking
        {
            get { return _isWorking; }

            set
            {
                if (_isWorking == value)
                {
                    return;
                }

                _isWorking = value;
                RaisePropertyChanged(IsWorkingPropertyName);
            }
        }

        #endregion

        #region States

        public const string StatesPropertyName = "States";

        private ObservableCollection<string> _states = new ObservableCollection<string>();

        public ObservableCollection<string> States
        {
            get { return _states; }

            set
            {
                if (_states == value)
                {
                    return;
                }

                _states = value;
                RaisePropertyChanged(StatesPropertyName);
            }
        }

        #endregion

        #region Counties

        public const string CountiesPropertyName = "Counties";

        private ObservableCollection<string> _counties = new ObservableCollection<string>();

        public ObservableCollection<string> Counties
        {
            get { return _counties; }

            set
            {
                if (_counties == value)
                {
                    return;
                }

                _counties = value;
                RaisePropertyChanged(CountiesPropertyName);

                SelectedCounty = Counties[0];
            }
        }

        #endregion

        #region Delay

        public const string DelayPropertyName = "Delay";

        private int _delay = 5;

        public int Delay
        {
            get { return _delay; }

            set
            {
                if (_delay == value)
                {
                    return;
                }

                _delay = value;
                RaisePropertyChanged(DelayPropertyName);
            }
        }

        #endregion

        #region LastExportPath

        public const string LastExportPathPropertyName = "LastExportPath";

        private string _lastExportPath = string.Empty;

        public string LastExportPath
        {
            get { return _lastExportPath; }

            set
            {
                if (_lastExportPath == value)
                {
                    return;
                }

                _lastExportPath = value;
                RaisePropertyChanged(LastExportPathPropertyName);
            }
        }

        #endregion


        #region GetParcelSamplesCommand

        private RelayCommand _getParcelSamplesCommand;

        public RelayCommand GetParcelSamplesCommand
        {
            get
            {
                return _getParcelSamplesCommand ??
                       (_getParcelSamplesCommand =
                           new RelayCommand(ExecuteGetParcelSamplesCommand, CanExecuteGetParcelSamplesCommand));
            }
        }

        private RelayCommand _testGeorgiaCommand;

        public RelayCommand TestGeorgiaCommand
        {
            get
            {
                return _testGeorgiaCommand ??
                       (_testGeorgiaCommand =
                           new RelayCommand(ExecuteTestGeorgiaCommand, CanExecuteTestGeorgiaCommand));
            }
        }

        private void ExecuteTestGeorgiaCommand()
        {
            StartProcess();
            ParcelsText = "";
            Task.Factory.StartNew(TestGeorgia);
        }

        private void ExecuteGetParcelSamplesCommand()
        {
            StartProcess();
            ParcelsText = "";
            Task.Factory.StartNew(GetGeorgiaSamples);
        }

        private void GetGeorgiaSamples()
        {
            Scraper scraper;
            try
            {
                scraper = _scraperFactory.GetScraper(StateCounty);
                scraper.NotifyEvent += ScraperOnNotifyEvent;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                StopProcess();
                return;
            }

            IList<string> parcels = new List<string>();
            try
            {
                parcels = ((ScraperGeorgia)scraper).GetParcelSamples("B");
                ParcelsText = string.Join(Environment.NewLine, parcels);
            }
            catch
            {
            }

            Thread.Sleep(1000);
            StopProcess();
            UpdateStatus("Done");
        }

        private void TestGeorgia()
        {
            var fileData = File.ReadAllLines(".\\test_data.csv");

            var testInfo = fileData.Select(f => (county: f.Split(',')[0], code: f.Split(',').Length == 2 ? f.Split(',')[1] : string.Empty)).Where(r => !string.IsNullOrWhiteSpace(r.code)).ToList();

            UpdateStatus("Getting test parcels..");

            var results = new StringBuilder();
            var error = false;
            var counter = 0;

            foreach (var test in testInfo)
            {

                UpdateStatus($"County: {test.county}, Parcel: {test.code}");
                try
                {
                    var scrapper = new ScraperQpublicNew(test.county);
                    var item = scrapper.Scrape(test.code);

                    if (item.MapNumber.Trim().ToLower().Replace(" ", "") != test.code.Trim().ToLower().Replace(" ", ""))
                    {
                        results.AppendLine($"County: {test.county}, Parcel: {test.code}");
                        error = true;
                        counter++;
                    }
                }
                catch (Exception)
                {
                    results.AppendLine($"County: {test.county}, Parcel: {test.code}");
                    error = true;
                    counter++;
                }
                Thread.Sleep(2000);
            }

            if (error)
            {
                MessageBox.Show($"{counter} errors in {testInfo.Count} counties. See failed counties below: \r\n\r\n{results.ToString()}");
            }
            else
            {
                MessageBox.Show("All Tests OK!");
            }

            //var scr = new ScraperQpublicNew();
            //var images1 = scr.GetImages("", "Appling", "B003 001");
            //var images2 = scr.GetImages("", "Baldwin", "023 029D");
            //var images3 = scr.GetImages("", "Bacon", "010 027002");
            //var images4 = scr.GetImages("", "Oconee", "B 03L 002B");

            //OutputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "testGeorgia.csv");
            //try
            //{
            //    File.Delete(OutputFile);
            //}
            //catch
            //{
            //}

            //var parcelList = new List<Tuple<string, string>>();
            //var count = 0;
            //UpdateStatus("Getting test parcels..");
            //foreach (var county in ContentViewModel.GeorgiaCounties)
            //{
            //    Scraper scraper = null;
            //    count++;
            //    try
            //    {
            //        //SelectedCounty = county.Key;
            //        scraper = _scraperFactory.GetScraper($"Georgia:{county.Key}");
            //        //scraper.NotifyEvent += ScraperOnNotifyEvent;
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(ex.Message);
            //        StopProcess();
            //        return;
            //    }

            //    IList<string> parcels = new List<string>();
            //    try
            //    {
            //        parcels = ((ScraperGeorgia)scraper).GetParcelSamples("B", true, county.Value);
            //        var rand = new Random();
            //        var item = parcels[rand.Next(parcels.Count)];
            //        parcelList.Add(Tuple.Create(county.Key, item));
            //        item = parcels[rand.Next(parcels.Count)];
            //        parcelList.Add(Tuple.Create(county.Key, item));

            //        //ParcelsText = string.Join(Environment.NewLine, parcels);
            //    }
            //    catch
            //    {
            //    }

            //    //Thread.Sleep(1000);
            //    //StopProcess();

            //}
            //UpdateStatus("Start scraping..");
            //ParcelsText = string.Join(Environment.NewLine, parcelList.Select(x => $"{x.Item1}:{x.Item2}"));
            //var grouppedList = parcelList.GroupBy(x => x.Item1, z => z.Item2).ToList();
            //var itemList = new List<Item>();
            //int currentIndex = 0;
            //foreach (var parcelCluster in grouppedList)
            //{
            //    ProcessParcelsEx(parcelCluster.ToList(), parcelCluster.Key, parcelList.Count, ref currentIndex);
            //    itemList.AddRange(Items);
            //    if (_stop)
            //    {
            //        break;
            //    }
            //}
            //UpdateStatus("Exporting items");

            //Export(itemList, "");

            ////DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Clear());
            //Thread.Sleep(1000);
            //Current = 0;
            StopProcess();
        }

        private bool CanExecuteGetParcelSamplesCommand()
        {
            return true;
        }
        private bool CanExecuteTestGeorgiaCommand()
        {
            return true;
        }

        #endregion

        #region ParcelsText

        public const string ParcelsTextPropertyName = "ParcelsText";

        private string _parcelsText = "";

        public string ParcelsText
        {
            get { return _parcelsText; }

            set
            {
                if (_parcelsText == value)
                {
                    return;
                }

                _parcelsText = value;
                RaisePropertyChanged(ParcelsTextPropertyName);
            }
        }

        #endregion

        #region ParcelsNotFoundText

        public const string ParcelsNotFoundTextPropertyName = "ParcelsNotFoundText";

        private string _parcelsNotFoundText;

        public string ParcelsNotFoundText
        {
            get { return _parcelsNotFoundText; }

            set
            {
                if (_parcelsNotFoundText == value)
                {
                    return;
                }

                _parcelsNotFoundText = value;
                RaisePropertyChanged(ParcelsNotFoundTextPropertyName);
            }
        }

        #endregion

        #region ParcelFormatExample

        public const string ParcelFormatExamplePropertyName = "ParcelFormatExample";

        private string _parcelFormatExample = "Format:";

        public string ParcelFormatExample
        {
            get { return _parcelFormatExample; }

            set
            {
                if (_parcelFormatExample == value)
                {
                    return;
                }

                _parcelFormatExample = value;
                RaisePropertyChanged(ParcelFormatExamplePropertyName);
            }
        }

        #endregion

        #region OutputFile

        public const string OutputFilePropertyName = "OutputFile";

        private string _outputFile;// = @"C:\Users\mahmuduman\Desktop\freelance Project\CTALookup\coconino.csv";

        public string OutputFile
        {
            get { return _outputFile; }

            set
            {
                if (_outputFile == value)
                {
                    return;
                }

                _outputFile = value;
                RaisePropertyChanged(OutputFilePropertyName);
            }
        }

        #endregion

        #region RecordsPerFile

        public const string RecordsPerFilePropertyName = "RecordsPerFile";

        private Int32 _recordsPerFile;

        public Int32 RecordsPerFile
        {
            get { return _recordsPerFile; }

            set
            {
                if (_recordsPerFile == value)
                {
                    return;
                }

                _recordsPerFile = value;
                RaisePropertyChanged(RecordsPerFilePropertyName);
            }
        }

        #endregion

        #region UseParcelsFromFile

        public const string UseParcelsFromFilePropertyName = "UseParcelsFromFile";

        private Boolean _UseParcelsFromFile;

        public Boolean UseParcelsFromFile
        {
            get { return _UseParcelsFromFile; }

            set
            {
                if (_UseParcelsFromFile == value)
                {
                    return;
                }

                _UseParcelsFromFile = value;
                RaisePropertyChanged(UseParcelsFromFilePropertyName);
            }
        }

        #endregion

        #region ImagesFolder

        public const string ImagesFolderPropertyName = "ImagesFolder";

        private string _imagesFolder;

        public string ImagesFolder
        {
            get { return _imagesFolder; }

            set
            {
                if (_imagesFolder == value)
                {
                    return;
                }

                _imagesFolder = value;
                RaisePropertyChanged(ImagesFolderPropertyName);
            }
        }

        #endregion

        #region PhotosSuffix

        public const string PhotosSuffixPropertyName = "PhotosSuffix";

        private string _photosSuffix;

        public string PhotosSuffix
        {
            get { return _photosSuffix; }

            set
            {
                if (_photosSuffix == value)
                {
                    return;
                }

                _photosSuffix = value;
                RaisePropertyChanged(PhotosSuffixPropertyName);
            }
        }

        #endregion

        #region SalesDate

        public const string SalesDatePropertyName = "SalesDate";

        private DateTime? _salesDate;

        public DateTime? SalesDate
        {
            get { return _salesDate; }

            set
            {
                if (_salesDate == value)
                {
                    return;
                }

                _salesDate = value;
                RaisePropertyChanged(SalesDatePropertyName);
            }
        }

        #endregion

        #region ImprovementThreshold

        public const string ImprovementThresholdPropertyName = "ImprovementThreshold";

        private decimal? _improvementThreshold;

        public decimal? ImprovementThreshold
        {
            get { return _improvementThreshold; }

            set
            {
                if (_improvementThreshold == value)
                {
                    return;
                }

                _improvementThreshold = value;
                RaisePropertyChanged(ImprovementThresholdPropertyName);
            }
        }

        #endregion

        #region CheckInGoogleMaps

        public const string CheckInGoogleMapsPropertyName = "CheckInGoogleMaps";
        public const string TestCurrentCountyName = "TestCurrentCounty";

        private bool _checkInGoogleMaps = true;

        public bool CheckInGoogleMaps
        {
            get { return _checkInGoogleMaps; }

            set
            {
                if (_checkInGoogleMaps == value)
                {
                    return;
                }

                _checkInGoogleMaps = value;
                RaisePropertyChanged(CheckInGoogleMapsPropertyName);
            }
        }

        private bool _testCurrentCounty = false;

        public bool TestCurrentCounty
        {
            get { return _testCurrentCounty; }

            set
            {
                if (_testCurrentCounty == value)
                {
                    return;
                }

                _testCurrentCounty = value;
                RaisePropertyChanged(TestCurrentCountyName);
            }
        }

        #endregion

        #region Status

        public const string StatusPropertyName = "Status";

        private string _status;

        public string Status
        {
            get { return _status; }

            set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                RaisePropertyChanged(StatusPropertyName);
            }
        }

        #endregion

        #region CurrentParcelText

        public const string CurrentParcelTextPropertyName = "CurrentParcelText";

        private string _currentParcelText;

        public string CurrentParcelText
        {
            get { return _currentParcelText; }

            set
            {
                if (_currentParcelText == value)
                {
                    return;
                }

                _currentParcelText = value;
                RaisePropertyChanged(CurrentParcelTextPropertyName);
            }
        }

        #endregion

        #region StatusCountText

        public const string StatusCountTextPropertyName = "StatusCountText";

        private string _statusCountText;

        public string StatusCountText
        {
            get { return _statusCountText; }

            set
            {
                if (_statusCountText == value)
                {
                    return;
                }

                _statusCountText = value;
                RaisePropertyChanged(StatusCountTextPropertyName);
            }
        }

        #endregion

        #region StartCommand

        private RelayCommand _startCommand;

        public RelayCommand StartCommand
        {
            get
            {
                return _startCommand ??
                       (_startCommand =
                           new RelayCommand(ExecuteStartCommand, CanExecuteStartCommand));
            }
        }

        private Scraper GetScraper()
        {
            Scraper scraper;
            if (SelectedState == "Maryland")
            {
                scraper = new GenericScraper(_marylandCounties[SelectedCounty]);
            }
            else if (SelectedState == "Georgia")
            {
                scraper = new ScraperQpublicNew(SelectedCounty);
            }
            else
            {
                scraper = _scraperFactory.GetScraper(StateCounty);
            }

            scraper = scraper.GetClone();

            scraper.Delay = Delay * 1000;
            scraper.NotifyEvent += ScraperOnNotifyEvent;

            return scraper;
        }

        private Scraper GetGeorgiaScraper(string county)
        {
            Scraper scraper;

            scraper = _scraperFactory.GetScraper(string.Format("Georgia:{0}", county));

            scraper = scraper.GetClone();

            scraper.Delay = Delay * 1000;
            scraper.NotifyEvent += ScraperOnNotifyEvent;

            return scraper;
        }

        private void ScraperOnNotifyEvent(string msg)
        {
            Status = msg;
        }

        private void ExecuteStartCommand()
        {
            if (string.IsNullOrEmpty(OutputFile))
            {
                MessageBox.Show("You must select an output file first");
                return;
            }

            if (!SalesDate.HasValue)
            {
                MessageBox.Show("Please fill Sales Date");
                return;
            }

            StartProcess();

            Task.Factory.StartNew(Run);
        }

        private void Run()
        {
            List<String> allparcels;
            if (UseParcelsFromFile)
            {
                allparcels = (new IOHandler()).GetLines(OutputFile, true);
                for (Int32 i = 0; i < allparcels.Count; ++i)
                {
                    allparcels[i] = allparcels[i].Replace("\"", "");
                }
            }
            else
            {
                allparcels = ParcelsText.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Trim() != "").Select(x => x.Trim()).ToList();
            }

            if (UseParcelsFromFile || RecordsPerFile == 0 || RecordsPerFile > allparcels.Count)
            {
                ProcessParcels(allparcels, 0);
                UpdateStatus("Exporting items");
                Export(Items);
                Thread.Sleep(1000);
            }
            else
            {
                Int32 counter = 1;
                while (allparcels.Count > 0)
                {
                    var parcels = allparcels.Take(RecordsPerFile).ToList();
                    allparcels = allparcels.Skip(RecordsPerFile).ToList();

                    ProcessParcels(parcels, counter);

                    UpdateStatus("Exporting items");
                    if (_stop)
                    {
                        break;
                    }

                    Export(Items, counter < 2 ? "" : "-" + counter.ToString());

                    DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Clear());
                    Thread.Sleep(1000);
                    Current = 0;
                    ++counter;

                }
            }
            StopProcess();


            if (_imagesSaved)
            {
                MessageBox.Show("Some images were retrieved");
            }
        }

        private void ProcessParcels(List<String> parcels, Int32 Part)
        {
            var eachThread = (int)Math.Ceiling((double)parcels.Count / ThreadsTotal);
            var lockMe = new object();

            Total = parcels.Count;
            if (Total <= 0)
            {
                return;
            }
            List<ImageInfo> expectedImages = new List<ImageInfo>();
            var keyOfImages = new List<KeyValuePair<Image, string>>();
            var partition = Partitioner.Create(0, parcels.Count, eachThread);
            var count = 0;
            for (int i = 0; i < parcels.Count; ++i)
            {
                var item = new Item() { MapNumber = "TEMP-" + parcels[i], Index = i + 1, County = CleanupCounty(_selectedCounty) };
                DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Add(item));
            }

            Parallel.ForEach(partition, (range, state) =>
            {
                var scraper = GetScraper();
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (_stop)
                    {
                        UpdateStatus("Stopped");
                        break;
                    }

                    var parcel = parcels[i];
                    lock (lockMe)
                    {
                        //UpdateStatusCount(Items.Count + 1, parcels.Count);//i + 1
                        UpdateStatusCount(count + 1, parcels.Count, Part);//i + 1
                        UpdateCurrentParcel(parcel);
                    }

                    Item item = null;
                    try
                    {
                        string originalParcel = parcel;
                        parcel = scraper.PreProcess(parcel);
                        item = scraper.Scrape(parcel);
                        if (item != null)
                        {
                            item.SalesDate = SalesDate?.ToString("MM/dd/yyyy") ?? string.Empty;
                            item.County = _selectedCounty;

                            var v = item.ImprovementValue?.Trim('$');

                            item.Interested = (ImprovementThreshold ?? -1) < Convert.ToDecimal(string.IsNullOrWhiteSpace(v) ? "0" : v) ? "Y" : "N";
                            item.State = GetStateCode(_selectedState);
                            item = PostProcess(item, originalParcel, scraper);
                        }
                    }
                    catch (Exception e)

                    {
                        Logger.Log(string.Format("An error ocurred with {0}: {1}", parcel, e));
                    }

                    Current += 1;

                    if (item == null)
                    {
                        lock (lockMe)
                        {
                            AddParcelError(parcel);
                        }
                    }
                    else
                    {
                        foreach (var image in item.Images)
                        {
                            if (image.Name == null || image.Name == "")
                            {
                                image.Name = parcel;
                            }
                            expectedImages.Add(image);
                        }
                        expectedImages.AddRange(scraper.GetImages(SelectedState, SelectedCounty, parcel));
                        if (item.Image != null)
                        {
                            lock (lockMe)
                            {
                                SaveImage(item.Image, parcel);
                                item.Image.Dispose();
                                item.Image = null;
                            }
                        }

                        lock (lockMe)
                        {
                            //DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Add(item));
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                item.Index = i;
                                Items[i - 1] = item;
                            });
                        }
                    }
                    ++count;
                }

            });

            var images = expectedImages.Where(a => a.Image.Width >= 200 && a.Image.Height >= 200).GroupBy(a => a.URL).Select(a => a.FirstOrDefault()).ToList();
            SaveAllImages(lockMe, images);
        }

        void SaveAllImages(object lockMe, List<ImageInfo> expectedImages)
        {
            var abc = GetABC();
            List<ImageInfo> imagesToSave = new List<ImageInfo>();
            foreach (var item in expectedImages.GroupBy(a => a.Name))
            {
                int i = 0;
                foreach (var sameImage in item)
                {
                    if (i == 0)
                    {
                        sameImage.Name = sameImage.Name + "-00";
                    }
                    else
                    {
                        sameImage.Name = sameImage.Name + "-00" + abc[i - 1];
                    }
                    i++;
                    imagesToSave.Add(sameImage);
                }
            }
            foreach (var imageInfo in imagesToSave)
            {
                lock (lockMe)
                {
                    SaveImage(imageInfo.Image, imageInfo.Name);
                }
            }
        }

        public List<string> GetABC()
        {
            return new List<string> { "a", "b", "c", "d", "e", "f", "g", "h", "i",
                "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t",
                "u", "v", "w", "x", "y", "z", };
        }

        private void ProcessParcelsEx(List<String> parcels, string county, int globalCount, ref int currentIndex)
        {
            var eachThread = (int)Math.Ceiling((double)parcels.Count / ThreadsTotal);
            var lockMe = new object();

            Total = globalCount;
            if (Total <= 0)
            {
                return;
            }

            var partition = Partitioner.Create(0, parcels.Count, eachThread);
            var count = 0;
            int localIndex = currentIndex;
            currentIndex += parcels.Count;

            for (int i = 0; i < parcels.Count; ++i)
            {
                var item = new Item() { MapNumber = "TEMP-" + parcels[i], Index = i + 1 + localIndex, County = CleanupCounty(county) };
                DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Add(item));
            }

            Parallel.ForEach(partition, (range, state) =>
            {
                var scraper = GetGeorgiaScraper(county);
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (_stop)
                    {
                        UpdateStatus("Stopped");
                        break;
                    }

                    var parcel = parcels[i];
                    lock (lockMe)
                    {
                        //UpdateStatusCount(Items.Count + 1, parcels.Count);//i + 1
                        UpdateStatusCount(Current + 1, globalCount, 0);

                        UpdateCurrentParcel(parcel);
                    }

                    Item item = null;
                    try
                    {
                        string originalParcel = parcel;
                        parcel = scraper.PreProcess(parcel);
                        item = scraper.Scrape(parcel);
                        item.County = CleanupCounty(county);
                        item.State = GetStateCode(_selectedState);
                        item = PostProcess(item, originalParcel, scraper);
                    }
                    catch (Exception e)

                    {
                        Logger.Log(string.Format("An error ocurred with {0}: {1}", parcel, e));
                    }

                    Current += 1;

                    if (item == null)
                    {
                        lock (lockMe)
                        {
                            AddParcelError(parcel);
                        }
                    }
                    else
                    {
                        if (item.Image != null)
                        {
                            lock (lockMe)
                            {
                                SaveImage(item.Image, parcel);
                                item.Image.Dispose();
                                item.Image = null;
                            }
                        }

                        lock (lockMe)
                        {
                            //DispatcherHelper.CheckBeginInvokeOnUI(() => Items.Add(item));
                            localIndex += i;

                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                item.Index = localIndex + 1;
                                Items[localIndex] = item;
                            });
                        }
                    }
                    ++count;
                }

            });

        }

        private void Export<T>(IList<T> items, String FileSuffix = "", Int32 startInd = 0)
        {
            var lastExportPath = string.Empty;

            var ioHandler = new IOHandler();
            if (FileSuffix == "")
            {
                if (UseParcelsFromFile)
                {
                    ioHandler.Export(items, OutputFile, startInd);
                }
                else
                {
                    ioHandler.Export(items, OutputFile);//startInd
                }

                lastExportPath = Path.GetDirectoryName(OutputFile);
            }
            else
            {
                var path = Path.GetDirectoryName(OutputFile);
                var fname = Path.GetFileNameWithoutExtension(OutputFile);
                var ext = Path.GetExtension(OutputFile);
                path += "\\" + fname + FileSuffix + ext;
                if (UseParcelsFromFile)
                {
                    ioHandler.Export(items, OutputFile, startInd);
                    lastExportPath = Path.GetDirectoryName(OutputFile);
                }
                else
                {
                    ioHandler.Export(items, path);
                    lastExportPath = Path.GetDirectoryName(path);
                }
            }

            SaveLastExportPath(lastExportPath);
            LastExportPath = lastExportPath;
        }

        private void SaveImage(Image image, string name)
        {
            string path = Path.Combine(ImagesFolder, name);
            path += ".jpg";
            if (!Directory.Exists(ImagesFolder))
            {
                Directory.CreateDirectory(ImagesFolder);
            }
            image.Save(path, ImageFormat.Jpeg);
            UpdateStatus(string.Format("Image saved: {0}", path));

            _imagesSaved = true;
        }

        private void AddParcelError(string parcel)
        {
            ParcelsNotFoundText += string.IsNullOrEmpty(ParcelsNotFoundText) ? parcel : Environment.NewLine + parcel;
        }

        private Item PostProcess(Item item, string parcel, Scraper scraper)
        {
            //If the item wasn't found.
            if (item.MapNumber?.ToLower() == "n/a" &&
                item.OwnerName?.ToLower() == "n/a")
            {
                return null;
            }

            scraper.PostProcess(item);

            //Always override the Map Number
            item.MapNumber = "TEMP-" + parcel;

            TrimFields(item);

            //if search for city in Google Maps
            if (CheckInGoogleMaps)
            {
                SearchInGoogleMaps(item);
            }
            scraper.SetAddressNotes(item);

            item.AdjustValues();
            return item;
        }

        private static string CleanupCounty(string county)
        {
            var replacements = new string[]
            {
                "(records there, but search not working)",
                "- Subscription Required",
                "(1 line results works, but detail mis-linked)",
                "(subscription)",
                "- New Website"
            };
            var cleanCounty = county ?? "";
            foreach (var replacement in replacements)
            {
                cleanCounty = cleanCounty.Replace(replacement, "");
            }
            cleanCounty = cleanCounty.TrimEnd();

            return cleanCounty;
        }

        private string GetGoogleMapsKeyword(Item item)
        {
            var county = CleanupCounty(_selectedCounty);
            return $"{item.PhysicalAddress1},{_selectedState} in county {county}";
        }

        private void SearchInGoogleMaps(Item item)
        {
            //If the city is already assigned
            if (!string.IsNullOrWhiteSpace(item.PhysicalAddressCity))
            {
                return;
            }

            //If the address is empty
            if (string.IsNullOrWhiteSpace(item.PhysicalAddress1) || item.PhysicalAddress1.Length <= 2)
            {
                return;
            }
            string keyword = GetGoogleMapsKeyword(item);
            UpdateStatus($"Searching '{keyword}' in Google Maps");

            var addr = _addressScraper.Scrape(keyword);

            if (addr != null)
            {

                if (item.PhysicalAddress1.ToUpper() == item.PhysicalAddress1)
                {
                    addr.City = addr.City?.ToUpper();
                }

                UpdateStatus($"City; found: {addr.City}");
                item.PhysicalAddressCity = addr.City;
                item.PhysicalAddressState = addr.State;
                item.PhysicalAddressZip = addr.Zip;
            }
            else
            {
                UpdateStatus(string.Format("Address wasn't found. Check log for details"));
            }
        }

        private void TrimFields(Item item)
        {
            item.Acreage = item.Acreage.Trim();
            item.Description = item.Description.Trim();
            item.HomesteadExcemption = item.HomesteadExcemption.Trim();
            item.ImprovementValue = item.ImprovementValue.Trim();
            item.LandValue = item.LandValue.Trim();
            item.LegalDescription = item.LegalDescription.Trim().Replace("\n", " ").Replace("\r", " ");
            item.MapNumber = item.MapNumber.Trim();
            item.MarketValue = item.MarketValue.Trim();
            item.OwnerAddress = item.OwnerAddress.Trim();
            item.OwnerCity = item.OwnerCity.Trim();
            item.OwnerFirstName = item.OwnerFirstName.Trim();
            item.OwnerLastName = item.OwnerLastName.Trim();
            item.OwnerMiddleInitial = item.OwnerMiddleInitial.Trim();
            item.OwnerName = item.OwnerName.Trim();
            item.OwnerResident = item.OwnerResident.Trim();
            item.OwnerState = item.OwnerState.Trim();
            item.OwnerZip = item.OwnerZip.Trim();
            item.PhysicalAddress1 = item.PhysicalAddress1.Trim();
            item.PhysicalAddressCity = item.PhysicalAddressCity.Trim();
            item.PhysicalAddressState = item.PhysicalAddressState.Trim();
            item.PhysicalAddressZip = item.PhysicalAddressZip.Trim();
            item.TransferDate = item.TransferDate.Trim();
            item.TransferPrice = item.TransferPrice.Trim();
            item.WaterfrontPropertyType = item.WaterfrontPropertyType.Trim();
            item.County = CleanupCounty(item.County);
        }
        private void UpdateCurrentParcel(string parcel)
        {
            CurrentParcelText = string.Format("Current: {0}", parcel);
        }

        private void UpdateStatus(string message)
        {
            Status = message.EndsWith("...") ? message : message + "...";
        }

        private void UpdateStatusCount(int index, int count, int part)
        {
            if (part == 0)
            {
                StatusCountText = string.Format("Record {0} of {1}", index, count);
            }
            else
            {
                StatusCountText = string.Format("Part {0} Record {1} of {2}", part, index, count);
            }
        }

        private void StartProcess()
        {
            _imagesSaved = false;
            _stop = false;
            ParcelsNotFoundText = "";
            IsWorking = true;
            Items = new ObservableCollection<Item>();
        }

        private void StopProcess()
        {
            IsWorking = false;
            CurrentParcelText = "";
            StatusCountText = "";
            Status = "Done";
            Current = 0;
        }

        private bool CanExecuteStartCommand()
        {
            return true;
        }

        #endregion

        #region ThreadsTotal

        public const string ThreadsTotalPropertyName = "ThreadsTotal";

        private int _threadsTotal;

        public int ThreadsTotal
        {
            get { return _threadsTotal; }

            set
            {
                if (_threadsTotal == value)
                {
                    return;
                }

                _threadsTotal = value;
                RaisePropertyChanged(ThreadsTotalPropertyName);
            }
        }

        #endregion

        #region StopCommand

        private RelayCommand _stopCommand;

        public RelayCommand StopCommand
        {
            get
            {
                return _stopCommand ??
                       (_stopCommand =
                           new RelayCommand(ExecuteStopCommand, CanExecuteStopCommand));
            }
        }

        private void ExecuteStopCommand()
        {
            _stop = true;
        }

        private bool CanExecuteStopCommand()
        {
            return true;
        }

        #endregion

        #region Current

        public const string CurrentPropertyName = "Current";

        private int _current = 0;

        public int Current
        {
            get { return _current; }

            set
            {
                if (_current == value)
                {
                    return;
                }

                _current = value;
                RaisePropertyChanged(CurrentPropertyName);
            }
        }

        #endregion

        #region Total

        public const string TotalPropertyName = "Total";

        private int _total;

        public int Total
        {
            get { return _total; }

            set
            {
                if (_total == value)
                {
                    return;
                }

                _total = value;
                RaisePropertyChanged(TotalPropertyName);
            }
        }

        #endregion

        #region SelectedState

        public const string SelectedStatePropertyName = "SelectedState";

        private string _selectedState;

        public string SelectedState
        {
            get { return _selectedState; }

            set
            {
                if (_selectedState == value)
                {
                    return;
                }

                _selectedState = value;
                RaisePropertyChanged(SelectedStatePropertyName);

                ChangeCounties();
            }
        }

        #endregion

        #region SelectedCounty

        public const string SelectedCountyPropertyName = "SelectedCounty";

        private string _selectedCounty;

        public string SelectedCounty
        {
            get { return _selectedCounty; }

            set
            {
                if (_selectedCounty == value)
                {
                    return;
                }

                _selectedCounty = value;
                RaisePropertyChanged(SelectedCountyPropertyName);

                GetParcelSamplesVisible = SelectedState == "Georgia" && SelectedCounty != "Chatham";
                UpdateParcelFormatExample();
                SetOutputPath();
            }
        }

        private static readonly IDictionary<string, string> ParcelSamples = new Dictionary<string, string>()
            {
                {"South Carolina:Aiken", "110-19-02-001"},
                {"South Carolina:York", "7200201016"},
                {"South Carolina:Georgetown", "03-0446-001-01-00"},
                {"South Carolina:Anderson", "104-00-03-001 or 02211402006"},
                {"South Carolina:Beaufort", "R100 016 000 0264 0000"},
                {"South Carolina:Charleston", "0280100032"},
                {"South Carolina:Dorchester", "1520602017"},
                {"South Carolina:Fairfield", "200-00-00-046-000"},
                {"South Carolina:Greenville", "T024010107100"},
                {"South Carolina:Greenwood", "6879-951-120"},
                {"South Carolina:Horry", "1850102008 or 16352-01-043"},
                {"South Carolina:Kershaw", "C299-16-00-002-SFY"},
                {"South Carolina:Laurens", "403-00-00-004"},
                {"South Carolina:Lexington", "003500-01-029"},
                {"South Carolina:Oconee", "052-00-01-018"},
                {"South Carolina:Orangeburg", "0210-00-04-004.000"},
                {"South Carolina:Pickens", "4065-14-44-4616"},
                {"South Carolina:Richland", "R02316-02-05"},
                {"South Carolina:Spartanburg", "5-11-00-032.18"},
                {"Maryland:BALTIMORE CITY", "01 05 1786 038"},
                {"Maryland:ANNE ARUNDEL", "04 105 04117700"},
                {"Maryland:CALVERT", "02 106906"},
                {"Maryland:DORCHESTER", "08 187452"},
                {"Maryland:FREDERICK", "09 306110"},
                {"Maryland:GARRETT", "05 007224"},
                {"Maryland:HOWARD", "01 318918"},
                {"Maryland:MONTGOMERY", "07 03611108"},
                {"Maryland:PRINCE GEORGE'S", "10 3043965"},
                {"Maryland:QUEEN ANNE'S", "04 029550"},
                {"Maryland:SOMERSET", "01 024809"},
                {"Maryland:WASHINGTON", "03 012530"},
                {"Maryland:TALBOT", "01 024396"},
                {"Maryland:WORCESTER", "02 024357"},
                {"Maryland:*", "04 013239..."},
                {"Georgia:Chatham", "1-00"},
                {"Georgia:Polk", "053-095-, 053-132A, 053C002-"},
                {"Arizona:Apache", "102-56-042 or 104-24-002J"},
                {"Arizona:Mohave", "111-18-095"},
                {"Arizona:Coconino", "11007047 or 11313002e"},
                {"Arizona:Maricopa", "101-10-110"},
                {"Arizona:Navajo", "208-10-010 or 103-01-001-A"},
                {"Arizona:Yavapai", "107-14-093D"},
                {"Arizona:Yuma", "70169001"},
                {"Arizona:La Paz", "306-10-008 or 306-22-007-D"},
                {"Arizona:Santa Cruz", "109-24-036"},
                {"Arizona:Graham", "109-14-037B"},
                {"Arizona:Gila", "204-16-039L"},
                {"Arizona:Pima", "101141620"},
                {"Arizona:Pinal", "512-45-3910"},
            };

        private void UpdateParcelFormatExample()
        {
            string format = "";
            ParcelSamples.TryGetValue(StateCounty, out format);

            if (string.IsNullOrEmpty(format))
            {
                string wildcard = string.Format("{0}:*", SelectedState);
                ParcelSamples.TryGetValue(wildcard, out format);
            }

            ParcelFormatExample = string.Format("Format: {0}", format);
        }

        #endregion

        #region GetParcelSamplesVisible

        public const string GetParcelSamplesVisiblePropertyName = "GetParcelSamplesVisible";

        private bool _getParcelSamplesVisible;
        private IAddressScraper _addressScraper;
        private ScraperFactory _scraperFactory;
        private static string[] _dwCounties;

        public bool GetParcelSamplesVisible
        {
            get { return _getParcelSamplesVisible; }

            set
            {
                if (_getParcelSamplesVisible == value)
                {
                    return;
                }

                _getParcelSamplesVisible = value;
                RaisePropertyChanged(GetParcelSamplesVisiblePropertyName);
            }
        }

        #endregion

        private void ChangeCounties()
        {
            switch (SelectedState)
            {
                case "Arizona":
                    Counties = new ObservableCollection<string>(_arizonaCounties);
                    break;
                case "South Carolina":
                    Counties = new ObservableCollection<string>(_southCarolinaCounties);
                    break;
                case "Maryland":
                    Counties = new ObservableCollection<string>(_marylandCounties.Keys);
                    break;
                case "Georgia":
                    Counties = new ObservableCollection<string>(_georgiaCounties.Keys);
                    break;
            }
        }

        private void PopulateGeorgiaCounties()
        {
            _georgiaCounties = GetGeorgiaCounties();
        }

        private static Dictionary<string, string> GetGeorgiaCounties()
        {
            return new Dictionary<string, string>
            {
                {"Appling", "ga_appling"},
                {"Atkinson", "ga_atkinson"},
                {"Bacon", "ga_bacon"},
                {"Baker", "ga_baker"},
                {"Baldwin", "ga_baldwin"},
                {"Banks", "ga_banks"},
                {"Barrow", "ga_barrow"},
                {"Bartow", "ga_bartow"},
                {"Ben Hill", "ga_benhill"},
                {"Berrien", "ga_berrien"},
                {"Bibb", "ga_bibb"},
                {"Bleckley", "ga_bleckley"},
                {"Brantley", "ga_brantley"},
                {"Brooks", "ga_brooks"},
                {"Bryan", "ga_bryan"},
                {"Bulloch", "ga_bulloch"},
                {"Burke", "ga_burke"},
                {"Butts", "ga_butts"},
                {"Calhoun", "ga_calhoun"},
                {"Camden", "ga_camden"},
                {"Candler", "ga_candler"},
                {"Carroll", "ga_carroll"},
                {"Catoosa", "ga_catoosa"},
                {"Charlton", "ga_charlton"},
                {"Chatham", "ga_chatham"},
                {"Chattahoochee", "ga_chattahoochee"},
                {"Chattooga", "ga_chattooga"},
                {"Cherokee", "ga_cherokee"},
                {"Clarke", "ga_clarke"},
                {"Clay", "ga_clay"},
                {"Clayton", "ga_clayton"}, //http://weba.co.clayton.ga.us/taxcgi-bin/wtx200r.pgm?parcel=13201C%20%20A013
                {"Clinch", "ga_clinch"},
                {"Cobb", "ga_cobb"},
                {"Coffee", "ga_coffee"},
                {"Colquitt", "ga_colquitt"},
                {"Columbia", "ga_columbia"},
                {"Cook", "ga_cook"},
                {"Coweta", "ga_coweta"},
                {"Crawford", "ga_crawford"},
                {"Crisp", "ga_crisp"},
                {"Dade", "ga_dade"},
                {"Dawson", "ga_dawson"},
                {"Decatur", "ga_decatur"},
                {"DeKalb", "ga_dekalb"},
                {"Dodge", "ga_dodge"},
                {"Dooly", "ga_dooly"},
                {"Dougherty", "ga_dougherty"},
                {"Douglas", "ga_douglas"},
                {"Early", "ga_early"},
                {"Echols", "ga_echols"},
                {"Effingham", "ga_effingham"},
                {"Elbert", "ga_elbert"},
                {"Emanuel", "ga_emanuel"},
                {"Evans", "ga_evans"},
                {"Fannin", "ga_fannin"},
                {"Fayette", "ga_fayette"},
                {"Floyd", "ga_floyd"},
                {"Forsyth", "ga_forsyth"},
                {"Franklin", "ga_franklin"},
                {"Fulton", "ga_fulton"},
                {"Gilmer", "ga_gilmer"},
                {"Glascock", "ga_glascock"},
                {"Glynn", "ga_glynn"},
                {"Gordon", "ga_gordon"},
                {"Grady", "ga_grady"},
                {"Greene", "ga_greene"},
                {"Gwinnett", "ga_gwinnett"},
                {"Habersham", "ga_habersham"},
                {"Hall", "ga_hall"},
                {"Hancock", "ga_hancock"},
                {"Haralson", "ga_haralson"},
                {"Harris", "ga_harris"},
                {"Hart", "ga_hart"},
                {"Heard", "ga_heard"},
                {"Henry", "ga_henry"},
                {"Houston", "ga_houston"},
                {"Irwin", "ga_irwin"},
                {"Jackson", "ga_jackson"},
                {"Jasper", "ga_jasper"},
                {"Jeff Davis", "ga_jeffdavis"},
                {"Jefferson", "ga_jefferson"},
                {"Jenkins", "ga_jenkins"},
                {"Johnson", "ga_johnson"},
                {"Jones", "ga_jones"},
                {"Lamar", "ga_lamar"},
                {"Lanier", "ga_lanier"},
                {"Laurens", "ga_laurens"},
                {"Lee", "ga_lee"},
                {"Liberty", "ga_liberty"},
                {"Lincoln", "ga_lincoln"},
                {"Long", "ga_long"},
                {"Lowndes", "ga_lowndes"},
                {"Lumpkin", "ga_lumpkin"},
                {"Macon", "ga_macon"},
                {"Madison", "ga_madison"},
                {"Marion", "ga_marion"},
                {"McDuffie", "ga_mcduffie"},
                {"McIntosh", "ga_mcintosh"},
                {"Meriwether", "ga_meriwether"},
                {"Miller", "ga_miller"},
                {"Mitchell", "ga_mitchell"},
                {"Monroe", "ga_monroe"},
                {"Montgomery", "ga_montgomery"},
                {"Morgan", "ga_morgan"},
                {"Murray", "ga_murray"},
                {"Muscogee", "ga_muscogee"},
                {"Newton", "ga_newton"},
                {"Oconee", "ga_oconee"},
                {"Oglethorpe", "ga_oglethorpe"},
                {"Paulding", "ga_paulding"},
                {"Peach", "ga_peach"},
                {"Pickens", "ga_pickens"},
                {"Pierce", "ga_pierce"},
                {"Pike", "ga_pike"},
                {"Polk", "ga_polk"},
                {"Pulaski", "ga_pulaski"},
                {"Putnam", "ga_putnam"},
                {"Quitman", "ga_quitman"},
                {"Rabun", "ga_rabun"},
                {"Randolph", "ga_randolph"},
                {"Richmond", "ga_richmond"},
                {"Rockdale", "ga_rockdale"},
                {"Schley", "ga_schley"},
                {"Screven", "ga_screven"},
                {"Seminole", "ga_seminole"},
                {"Spalding", "ga_spalding"},
                {"Stephens", "ga_stephens"},
                {"Stewart", "ga_stewart"},
                {"Sumter", "ga_sumter"},
                {"Talbot", "ga_talbot"},
                {"Taliaferro", "ga_taliaferro"},
                {"Tattnall", "ga_tattnall"},
                {"Taylor", "ga_taylor"},
                {"Telfair", "ga_telfair"},
                {"Terrell", "ga_terrell"},
                {"Thomas", "ga_thomas"},
                {"Tift", "ga_tift"},
                {"Toombs", "ga_toombs"},
                {"Towns", "ga_towns"},
                {"Treutlen", "ga_treutlen"},
                {"Troup", "ga_troup"},
                {"Turner", "ga_turner"},
                {"Twiggs", "ga_twiggs"},
                {"Union", "ga_union"},
                {"Upson", "ga_upson"},
                {"Walker", "ga_walker"},
                {"Walton", "ga_walton"},
                {"Ware", "ga_ware"},
                {"Warren", "ga_warren"},
                {"Washington", "ga_washington"},
                {"Wayne", "ga_wayne"},
                {"Webster", "ga_webster"},
                {"Wheeler", "ga_wheeler"},
                {"White", "ga_white"},
                {"Wilcox", "ga_wilcox"},
                {"Wilkes", "ga_wilkes"},
                {"Wilkinson", "ga_wilkinson"},
                {"Worth", "ga_worth"}
            };
        }

        private void PopulateMarylandCounties()
        {
            _marylandCounties = new Dictionary<string, string>
                {
                    {"ALLEGANY", "01"},
                    {"ANNE ARUNDEL", "02"},
                    {"BALTIMORE CITY", "03"},
//                        {"BALTIMORE", "04"},
                    {"CALVERT", "05"},
                    {"CAROLINE", "06"},
                    {"CARROLL", "07"},
                    {"CECIL", "08"},
                    {"CHARLES", "09"},
                    {"DORCHESTER", "10"},
                    {"FREDERICK", "11"},
                    {"GARRETT", "12"},
                    {"HARFORD", "13"},
                    {"HOWARD", "14"},
                    {"KENT", "15"},
                    {"MONTGOMERY", "16"},
                    {"PRINCE GEORGE'S", "17"},
                    {"QUEEN ANNE'S", "18"},
                    {"ST. MARY'S", "19"},
                    {"SOMERSET", "20"},
                    {"TALBOT", "21"},
                    {"WASHINGTON", "22"},
                    {"WICOMICO", "23"},
                    {"WORCESTER", "24"}
                };
        }

        private void SetOutputPath()
        {
            var basePath = LastExportPath;

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = $"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new char[1] { '\\' })}\\Output";
            }

            OutputFile = $"{basePath}\\{SelectedCounty} Results File.csv";
        }

        private void SaveLastExportPath(string lastExportPath)
        {
            var config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            config.AppSettings.Settings.Remove("LastExportPath");
            config.AppSettings.Settings.Add("LastExportPath", lastExportPath);

            config.Save(ConfigurationSaveMode.Modified);
        }

        public override void Cleanup() { }
    }
}