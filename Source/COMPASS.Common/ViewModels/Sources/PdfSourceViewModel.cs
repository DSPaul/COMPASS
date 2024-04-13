using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using ImageMagick;
using ImageMagick.Formats;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    public class PdfSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.PDF;
        public override bool IsValidSource(Codex codex) => IOService.IsPDFFile(codex.Path);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            Debug.Assert(IsValidSource(codex), "Codex without pdf found in pdf source");
            PdfDocument? pdfDoc = null;
            try
            {
                var info = await Task.Run(() =>
                {
                    PdfReader pdfReader = new(codex.Path);
                    pdfDoc = new PdfDocument(pdfReader);
                    return pdfDoc.GetDocumentInfo();
                });

                codex.Title = info.GetTitle() ?? String.Empty;
                if (info.GetAuthor() is not null)
                {
                    codex.Authors = new() { info.GetAuthor() };
                }
                codex.PageCount = pdfDoc!.GetNumberOfPages();

                // If it already has an ISBN, no need to check again
                if (!String.IsNullOrEmpty(codex.ISBN)) return codex;

                //Search for ISBN number in first 5 pages
                for (int page = 1; page <= Math.Min(5, pdfDoc.GetNumberOfPages()); page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                    //strip text of spaces
                    pageContent = Constants.RegexWhitespace().Replace(pageContent, "");
                    //search ISBN
                    string isbn = Constants.RegexISBN().Match(pageContent).Value;
                    if (!String.IsNullOrEmpty(isbn))
                    {
                        codex.ISBN = isbn;
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
                ProgressVM.AddLogEntry(logEntry);
            }

            finally { pdfDoc?.Close(); }

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            //return false if file doesn't exist
            if (!IOService.IsPDFFile(codex.Path) ||
                !File.Exists(codex.Path))
            {
                return false;
            }

            try //image.Read can throw exception if file can not be opened/read
            {
                using MagickImage image = new();
                await image.ReadAsync(codex.Path, ReadSettings);
                image.Format = MagickFormat.Png;
                image.BackgroundColor = new MagickColor("#000000"); //set background color as transparent
                image.Trim(); //cut off all transparency

                await image.WriteAsync(codex.CoverArt);
                CoverService.CreateThumbnail(codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to generate cover from {Path.GetFileName(codex.Path)}", ex);
                LogEntry logEntry = new(LogEntry.MsgType.Warning, $"Failed to generate cover from {codex.Title}");
                ProgressVM.AddLogEntry(logEntry);
                return false;
            }
        }

        private static readonly PdfReadDefines PDFReadDefines = new()
        {
            HideAnnotations = true,
            UseCropBox = true,
        };

        //Lazy load read Settings and make it static because takes a lot of time to construct according to profiler
        private static MagickReadSettings? _readSettings;
        private static MagickReadSettings ReadSettings => _readSettings ??= new()
        {
            Density = new Density(100),
            FrameIndex = 0, // First page
            FrameCount = 1, // Number of pages
            Defines = PDFReadDefines,
        };
    }
}
