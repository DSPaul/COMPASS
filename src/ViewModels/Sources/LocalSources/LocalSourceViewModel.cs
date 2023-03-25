using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public abstract class LocalSourceViewModel : SourceViewModel
    {
        public override string ProgressText => $"Import in Progress: File {ProgressCounter + 1} / {ImportAmount}";

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            codex.Title = Path.GetFileNameWithoutExtension(codex.Path);

            string FileType = Path.GetExtension(codex.Path);
            switch (FileType)
            {
                case ".pdf":
                    PdfDocument pdfDoc = null;
                    try
                    {
                        pdfDoc = new(new PdfReader(codex.Path));
                        var info = pdfDoc.GetDocumentInfo();
                        if (!String.IsNullOrWhiteSpace(info.GetTitle()))
                        {
                            codex.Title = info.GetTitle();
                        }
                        if (!String.IsNullOrWhiteSpace(info.GetAuthor()))
                        {
                            codex.Authors = new() { info.GetAuthor() };
                        }
                        codex.PageCount = pdfDoc.GetNumberOfPages();

                        //Search for ISBN number in first 5 pages
                        for (int page = 1; page <= Math.Min(5, pdfDoc.GetNumberOfPages()); page++)
                        {
                            ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                            string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                            //strip text of spaces
                            pageContent = Constants.RegexWhitespace().Replace(pageContent, "");
                            //search ISBN
                            string ISBN = Constants.RegexISBN().Match(pageContent)?.Value;
                            if (!String.IsNullOrEmpty(ISBN))
                            {
                                //Remove the "ISBN" so only number remains
                                ISBN = Constants.RegexISBNNumberOnly().Match(pageContent).Value;
                                codex.ISBN = ISBN;
                                codex = await new ISBNSourceViewModel().SetMetaData(codex);
                                break;
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        //in case pdf is corrupt: PdfReader will throw error
                        //in those cases: import the pdf without opening it
                        Logger.Error($"Failed to read metadata from {Path.GetFileName(codex.Path)}", ex);
                        LogEntry logEntry = new(LogEntry.MsgType.Warning, $"Failed to read metadata from {codex.Title}");
                        ProgressChanged(logEntry);
                    }

                    finally { pdfDoc?.Close(); }
                    break;

                default:
                    break;
            }

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex) => await Task.Run(() => CoverFetcher.GetCoverFromFile(codex));

        public async void ImportFiles(List<string> paths, bool showProgressWindow)
        {
            //filter out files already in collection
            IEnumerable<string> existingPaths = MainViewModel.CollectionVM.CurrentCollection.AllCodices.Select(codex => codex.Path);
            paths = paths.Except(existingPaths).ToList();

            ProgressCounter = 0;
            ImportAmount = paths.Count;

            List<Codex> newCodices = new();

            //make new codices first so they all have a valid ID
            foreach (string path in paths)
            {
                Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection) { Path = path };
                newCodices.Add(newCodex);
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
            }

            if (showProgressWindow)
            {
                ProgressWindow window = GetProgressWindow();
                window.Show();
            }

            var result = await Task.Run(() => Parallel.ForEach(newCodices, ImportCodex));
        }
    }
}
