using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Windows.System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjectCallisto
{
    class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Document> Documents 
        {
            get { return documents; }
            set
            {
                documents = value;
                OnPropertyChanged("Documents");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public StandardUICommand DeleteCommand { get; set; } = new StandardUICommand(StandardUICommandKind.Delete);
        public bool InfoBarIsOpen
        {
            get { return infoBarIsOpen; }
            set
            {
                infoBarIsOpen = value;
                OnPropertyChanged("InfoBarIsOpen");
            }
        }
        bool infoBarIsOpen = false;
        ObservableCollection<Document> documents = new ObservableCollection<Document>();
        StorageFile ResultDocument { get; set; }
        Document SelectedDocument { get; set; }

        FileOpenPicker CreateFileOpenPicker()
        {
            var openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.CommitButtonText = "Open";
            openPicker.FileTypeFilter.Clear();
            openPicker.FileTypeFilter.Add(".pdf");

            return openPicker;
        }

        FileSavePicker CreateFileSavePicker()
        {
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF document", new List<string>() { ".pdf" });
            savePicker.SuggestedFileName = "Merged_document";
            savePicker.CommitButtonText = "Save";

            return savePicker;
        }

        public async void SelectDocuments()
        {
            var openPicker = CreateFileOpenPicker();
            var files = await openPicker.PickMultipleFilesAsync();

            var documents = await Document.CreateDocumentsAsync(files);

            foreach (var document in documents)
            {
                await document.LoadPdfCoverAsync();
            }

            foreach (var document in documents)
            {
                Documents.Add(document);
            }
        }

        public async void MergeDocuments()
        {
            var savePicker = CreateFileSavePicker();
            var newDocument = await savePicker.PickSaveFileAsync();

            if (newDocument != null)
            {
                using (var mergedDocument = await Document.MergeDocumentsAsync(Documents))
                {
                    if (mergedDocument.PageCount > 0)
                    {
                        using (var stream = await newDocument.OpenStreamForWriteAsync())
                        {
                            mergedDocument.Save(stream);
                        }

                        ResultDocument = newDocument;
                        InfoBarIsOpen = true;
                    }
                }
            }
        }

        public void ClearDocuments()
        {
            if (Documents.Count > 0)
            {
                Documents.Clear();
            }
        }

        public async void OpenDocument()
        {
            var success = await Launcher.LaunchFileAsync(ResultDocument);

            if (success)
            {
                InfoBarIsOpen = false;
            }
        }

        public void DocumentsGotFocus(Document document)
        {
            SelectedDocument = document;
        }

        public void DocumentsLoaded()
        {
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;
        }

        void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (SelectedDocument != null)
            {
                Documents.Remove(SelectedDocument);
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
