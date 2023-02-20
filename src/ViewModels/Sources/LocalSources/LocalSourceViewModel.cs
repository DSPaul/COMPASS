using COMPASS.Models;
using COMPASS.Tools;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace COMPASS.ViewModels
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
                        PdfDocument pdfdoc = new(new PdfReader(codex.Path));
                        var info = pdfdoc.GetDocumentInfo();
                        if (!String.IsNullOrWhiteSpace(info.GetTitle()))
                        {
                            codex.Title = info.GetTitle();
                        }
                        if (!String.IsNullOrWhiteSpace(info.GetAuthor()))
                        {
                            codex.Authors = new() { info.GetAuthor() };
                        }
                        codex.PageCount = pdfdoc.GetNumberOfPages();
                        pdfdoc.Close();
                    }

                    catch (Exception ex)
                    {
                        Logger.log.Error(ex.InnerException);
                        //in case pdf is corrupt: PdfReader will throw error
                        //in those cases: import the pdf without opening it
                        LogEntry logEntry = new(LogEntry.MsgType.Warning, $"Failed to read metadata from {codex.Title}");
                        worker.ReportProgress(ProgressCounter, logEntry);
                    }

                    break;

                default:
                    break;
            }
            return codex;
        }

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
                bool succes = CoverFetcher.GetCoverFromFile(newCodex);
                if (!succes)
                {
                    logEntry = new(LogEntry.MsgType.Warning, $"Failed to generate thumbnail from {Path.GetFileName(path)}");
                    worker.ReportProgress(ProgressCounter, logEntry);
                }

                //Complete Import
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);
                ProgressCounter++;
                importWorker.ReportProgress(ProgressCounter);
            }
        }
    }
}
