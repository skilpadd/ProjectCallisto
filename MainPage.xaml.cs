using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ProjectCallisto
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<PdfDocument> Documents = new List<PdfDocument>();
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
        }

        private async void CreateDocuments()
        {
            if (Files != null)
            {
                foreach (var file in Files)
                {
                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        Documents.Add(PdfReader.Open(stream, PdfDocumentOpenMode.Import));
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
                    foreach (var page in document.Pages)
                    {
                        resultDocument.AddPage(page);
                    }
                    document.Close();
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
                using (var mergedDocument = MergeDocuments())
                {
                    using (var stream = await newDocument.OpenStreamForWriteAsync())
                    {
                        mergedDocument.Save(stream, true);
                    }
                }
            }
        }
    }
}
