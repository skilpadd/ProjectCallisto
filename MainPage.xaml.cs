using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace ProjectCallisto
{
    public sealed partial class MainPage : Page
    {
        ObservableCollection<Document> Documents = new ObservableCollection<Document>();
        IReadOnlyList<StorageFile> Files { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.CommitButtonText = "Open";
            openPicker.FileTypeFilter.Clear();
            openPicker.FileTypeFilter.Add(".pdf");

            Files = await openPicker.PickMultipleFilesAsync();
            CreateDocuments();

            DocumentsListView.ItemsSource = Documents;
        }

        private async void CreateDocuments()
        {
            if (Files != null)
            {
                foreach (var file in Files)
                {
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        var document = new Document(PdfReader.Open(stream, PdfDocumentOpenMode.Import));
                        document.Name = file.DisplayName;
                        document.DateCreated = file.DateCreated;
                        document.Path = file.Path;
                        Documents.Add(document);
                    }
                }
            }
        }

        private PdfDocument MergeDocuments()
        {
            var resultDocument = new PdfDocument();

            if (Documents != null)
            {
                foreach (var document in Documents)
                {
                    foreach (var page in document.Pdf.Pages)
                    {
                        resultDocument.AddPage(page);
                    }
                    document.Pdf.Close();
                }
            }

            return resultDocument;
        }

        private async void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("PDF document", new List<string>() { ".pdf" });
            savePicker.SuggestedFileName = "Merged_document";
            savePicker.CommitButtonText = "Save";

            var newDocument = await savePicker.PickSaveFileAsync();
            if (newDocument != null)
            {
                using (var mergedDocument = await Task.Run(() => MergeDocuments()))
                {
                    using (var stream = await newDocument.OpenStreamForWriteAsync())
                    {
                        mergedDocument.Save(stream, true);
                    }
                }
            }
        }
    }

    class Document
    {
        public PdfDocument Pdf { get; }
        public int PageCount { get; }
        public long FileSize { get; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTimeOffset DateCreated { get; set; }

        public Document(PdfDocument pdfDocument)
        {
            Pdf = pdfDocument;
            PageCount = Pdf.PageCount;
        }
    }
}
