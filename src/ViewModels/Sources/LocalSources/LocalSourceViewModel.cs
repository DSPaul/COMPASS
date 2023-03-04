using COMPASS.Models;
using COMPASS.Tools;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace COMPASS.ViewModels.Sources
{
    public abstract class LocalSourceViewModel : SourceViewModel
    {
        public override string ProgressText => $"Import in Progress: File {ProgressCounter + 1} / {ImportAmount}";

        public override Codex SetMetaData(Codex codex)
        {
            codex.Title = Path.GetFileNameWithoutExtension(codex.Path);

            string FileType = Path.GetExtension(codex.Path);
            switch (FileType)
            {
                case ".pdf":
                    try
                    {
                        PdfDocument pdfDoc = new(new PdfReader(codex.Path));
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
                        for (int page = 1; page <= Math.Max(5, pdfDoc.GetNumberOfPages()); page++)
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
                                codex = new ISBNSourceViewModel().SetMetaData(codex);
                                break;
                            }
                        }

                        pdfDoc.Close();
                    }

                    catch (Exception ex)
                    {
                        //in case pdf is corrupt: PdfReader will throw error
                        //in those cases: import the pdf without opening it
                        Logger.Error($"Failed to read metadata from {Path.GetFileName(codex.Path)}", ex);
                        LogEntry logEntry = new(LogEntry.MsgType.Warning, $"Failed to read metadata from {codex.Title}");
                        worker.ReportProgress(ProgressCounter, logEntry);
                    }

                    break;

                default:
                    break;
            }
            return codex;
        }

        public override bool FetchCover(Codex codex) => CoverFetcher.GetCoverFromFile(codex);

        protected void ImportFilePaths(object sender, DoWorkEventArgs e)
        {
            IEnumerable<string> paths = (IEnumerable<string>)e.Argument;
            BackgroundWorker importWorker = sender as BackgroundWorker;
            LogEntry logEntry = null;

            foreach (var path in paths)
            {
                //if already in collection, skip
                if (MainViewModel.CollectionVM.CurrentCollection.AllCodices.Any(codex => codex.Path == path))
                {
                    logEntry = new(LogEntry.MsgType.Warning, $"Skipped {Path.GetFileName(path)}, already imported");
                    Logger.Info($"Skipped {Path.GetFileName(path)}, already in collection");
                    ProgressCounter++;
                    importWorker.ReportProgress(ProgressCounter, logEntry);
                    continue;
                }

                //Start the Import
                logEntry = new(LogEntry.MsgType.Info, $"Importing {Path.GetFileName(path)}");
                importWorker.ReportProgress(ProgressCounter, logEntry);

                Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection)
                {
                    Path = path,
                };

                //Get metadata from file
                newCodex = SetMetaData(newCodex);

                //Get Cover from file
                bool succes = FetchCover(newCodex);
                if (!succes)
                {
                    logEntry = new(LogEntry.MsgType.Warning, $"Failed to generate thumbnail from {Path.GetFileName(path)}");
                    worker.ReportProgress(ProgressCounter, logEntry);
                }

                //Complete Import
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
                ProgressCounter++;
                importWorker.ReportProgress(ProgressCounter);
                Logger.Info($"Imported {newCodex.Title}");
            }
        }
    }
}
