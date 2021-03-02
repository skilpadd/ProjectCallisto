using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using WinPdf = Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace ProjectCallisto
{
    class Document
    {
        public PdfDocument Pdf { get; }
        private WinPdf.PdfDocument WnPdf { get; }
        public BitmapImage PdfCover { get; set; }
        public int PageCount { get; }
        public long FileSize { get; }
        public string Name { get; }
        public string Path { get; }
        public DateTimeOffset DateCreated { get; }

        public Document(StorageFile file)
        {
            Pdf = CreateDocumentAsync(file).Result;
            WnPdf = OpenWnPdfAsync(file).Result;
            PageCount = Pdf.PageCount;
            Name = file.DisplayName;
            DateCreated = file.DateCreated;
            Path = file.Path;
        }

        async Task<PdfDocument> CreateDocumentAsync(StorageFile file)
        {
            PdfDocument pdfDocument;
            using (var stream = await file.OpenStreamForReadAsync())
            {
                pdfDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
            }
            return pdfDocument;
        }

        async Task<WinPdf.PdfDocument> OpenWnPdfAsync(StorageFile file)
        {
            var document = await WinPdf.PdfDocument.LoadFromFileAsync(file);
            return document;
        }

        public async Task LoadPdfCoverAsync()
        {
            var page = WnPdf.GetPage(0);
            var image = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                await page.RenderToStreamAsync(stream);
                await image.SetSourceAsync(stream);
            }

            PdfCover = image;
        }

        public static List<Document> CreateDocuments(IEnumerable<StorageFile> files)
        {
            var documents = files.AsParallel().AsOrdered().Select(file => new Document(file)).ToList();
            return documents;
        }

        public static async Task<List<Document>> CreateDocumentsAsync(IEnumerable<StorageFile> files)
        {
            return await Task.Run(() => CreateDocuments(files));
        }

        public static PdfDocument MergeDocuments(IEnumerable<Document> documents)
        {
            var resultDocument = new PdfDocument();

            foreach (var document in documents)
            {
                foreach (var page in document.Pdf.Pages)
                {
                    resultDocument.AddPage(page);
                }
            }

            return resultDocument;
        }

        public static async Task<PdfDocument> MergeDocumentsAsync(IEnumerable<Document> documents)
        {
            return await Task.Run(() => MergeDocuments(documents));
        }
    }
}
