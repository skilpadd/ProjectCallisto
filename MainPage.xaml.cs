using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            var documents = await CreateDocumentsAsync();
            foreach (var document in documents)
            {
                Documents.Add(document);
            }
        }

        private List<Document> CreateDocuments()
        {
            var documents = new List<Document>();
            var tasks = Files.AsParallel().Select(async file => await CreateDocumentAsync(file)).ToList();
            foreach (var task in tasks)
            {
                documents.Add(task.Result);
            }

            return documents;
        }

        private async Task<List<Document>> CreateDocumentsAsync()
        {
            return await Task.Run(() => CreateDocuments());
        }

        private async Task<Document> CreateDocumentAsync(StorageFile file)
        {
            using (var stream = await file.OpenStreamForReadAsync())
            {
                var document = new Document(PdfReader.Open(stream, PdfDocumentOpenMode.Import))
                {
                    Name = file.DisplayName,
                    DateCreated = file.DateCreated,
                    Path = file.Path
                };
                return document;
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

        private async Task<PdfDocument> MergeDocumentsAsync()
        {
            return await Task.Run(() => MergeDocuments());
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
                using (var mergedDocument = await MergeDocumentsAsync())
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
