﻿using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace ProjectCallisto
{
    public sealed partial class MainPage : Page
    {
        ObservableCollection<Document> Documents = new ObservableCollection<Document>();
        IReadOnlyList<StorageFile> Files { get; set; }
        StorageFile ResultDocument { get; set; }
        StandardUICommand DeleteCommand { get; set; } = new StandardUICommand(StandardUICommandKind.Delete);
        Document SelectedDocument { get; set; }

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
            var documents = Files.AsParallel().AsOrdered().Select(file => CreateDocument(file)).ToList();
            return documents;
        }

        private async Task<List<Document>> CreateDocumentsAsync()
        {
            return await Task.Run(() => CreateDocuments());
        }

        private Document CreateDocument(StorageFile file)
        {
            var document = new Document(file);
            return document;
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

        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (SelectedDocument != null)
            {
                Documents.Remove(SelectedDocument);
            }
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
                    if (mergedDocument.PageCount > 0)
                    {
                        using (var stream = await newDocument.OpenStreamForWriteAsync())
                        {
                            mergedDocument.Save(stream, true);
                        }

                        ResultDocument = newDocument;
                        DocumentInfoBar.IsOpen = true;
                    }
                }
            }
        }

        private async void InfoBarButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Launcher.LaunchFileAsync(ResultDocument);

            if (success)
            {
                DocumentInfoBar.IsOpen = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (Documents.Count > 0)
            {
                Documents.Clear();
            }
        }

        private void DocumentsListView_Loaded(object sender, RoutedEventArgs e)
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;
        }

        private void DocumentsListView_GotFocus(object sender, RoutedEventArgs e)
        {
            SelectedDocument = (e.OriginalSource as ListViewItem).Content as Document;
        }
    }

    class Document
    {
        public PdfDocument Pdf { get; }
        public int PageCount { get; }
        public long FileSize { get; }
        public string Name { get; }
        public string Path { get; }
        public DateTimeOffset DateCreated { get; }

        public Document(StorageFile file)
        {
            Pdf = CreateDocument(file).Result;
            PageCount = Pdf.PageCount;
            Name = file.DisplayName;
            DateCreated = file.DateCreated;
            Path = file.Path;
        }

        async Task<PdfDocument> CreateDocument(StorageFile file)
        {
            PdfDocument pdfDocument;
            using (var stream = await file.OpenStreamForReadAsync())
            {
                pdfDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            }
            return pdfDocument;
        }
    }
}
