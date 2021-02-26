using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ProjectCallisto
{
    public sealed partial class MainPage : Page
    {
        MainViewModel ViewModel { get; set; } = new MainViewModel();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectDocuments();
        }

        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MergeDocuments();
        }

        private void InfoBarButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenDocument();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearDocuments();
        }

        private void DocumentsListView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.DocumentsLoaded();
        }

        private void DocumentsListView_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedDocument = (e.OriginalSource as ListViewItem).Content as Document;
            ViewModel.DocumentsGotFocus(selectedDocument);
        }
    }
}
