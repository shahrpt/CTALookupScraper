using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace CTALookup.ViewModel {
    public class MainViewModel : ViewModelBase {
        public MainViewModel() {
            for (int i = 0; i < 5; i++) {
                Titles.Add(string.Format("Tab {0}", i+1));
                var vm = new ContentViewModel() {Id = i + 1};
                vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == ContentViewModel.SelectedCountyPropertyName) {
                            var index = ContentViewModels.IndexOf(vm);
                            string value = vm.SelectedCounty;
                            Titles[index] = string.IsNullOrEmpty(value) ? string.Format("Tab {0}", index + 1) : value;
                        }
                    };
                ContentViewModels.Add(vm);
            }
        }

        #region ContentViewModels

        public const string ContentViewModelsPropertyName = "ContentViewModels";

        private ObservableCollection<ContentViewModel> _contentViewModels = new ObservableCollection<ContentViewModel>();

        public ObservableCollection<ContentViewModel> ContentViewModels {
            get { return _contentViewModels; }

            set {
                if (Equals(_contentViewModels, value)) {
                    return;
                }

                _contentViewModels = value;
                RaisePropertyChanged(ContentViewModelsPropertyName);
            }
        }

        #endregion

        #region Titles

        public const string TitlesPropertyName = "Titles";

        private ObservableCollection<string> _titles = new ObservableCollection<string>();

        public ObservableCollection<string> Titles {
            get { return _titles; }

            set {
                if (_titles == value) {
                    return;
                }

                _titles = value;
                RaisePropertyChanged(TitlesPropertyName);
            }
        }

        #endregion

    }
}