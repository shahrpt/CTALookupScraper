using System.Diagnostics.CodeAnalysis;
using CTALookup.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace CTALookup.ViewModel {
    public class ViewModelLocator {
        static ViewModelLocator() {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (ViewModelBase.IsInDesignModeStatic) {
                SimpleIoc.Default.Register<IDataService, Design.DesignDataService>();
            }
            else {
                SimpleIoc.Default.Register<IDataService, DataService>();
            }

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ContentViewModel>();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainViewModel Main {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

        #region Content

        [SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public ContentViewModel Content {
            get {
                return ServiceLocator.Current.GetInstance<ContentViewModel>();
            }
        }

        public static void ClearContent() {
            ServiceLocator.Current.GetInstance<ContentViewModel>().Cleanup();
        }

        #endregion

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup() {
            ClearContent();
        }
    }
}