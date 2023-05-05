using COMPASS.Models;
using COMPASS.Tools;
using ImageMagick;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class PdfSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.PDF;

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            PdfDocument pdfDoc = null;
            try
            {
                PdfReader pdfReader = new(codex.Path);
                pdfDoc = new(pdfReader); // This takes the most time but putting it on another thread doesnt seem to save much time
                //pdfDoc = await Task.Run(() => new PdfDocument(pdfReader)); 
                var info = pdfDoc.GetDocumentInfo();

                codex.Title = info.GetTitle();
                if (info.GetAuthor() is not null)
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
            if (String.IsNullOrEmpty(codex.Path) ||
                Path.GetExtension(codex.Path) != ".pdf" ||
                !File.Exists(codex.Path)) return false;

            var pdfReadDefines = new ImageMagick.Formats.PdfReadDefines()
            {
                HideAnnotations = true,
                UseCropBox = true,
            };

            MagickReadSettings settings = new()
            {
                Density = new Density(100, 100),
                FrameIndex = 0, // First page
                FrameCount = 1, // Number of pages
                Defines = pdfReadDefines,
            };

            try //image.Read can throw exception if file can not be opened/read
            {
                using MagickImage image = new();
                image.Read(codex.Path, settings);
                image.Format = MagickFormat.Png;
                image.BackgroundColor = new MagickColor("#000000"); //set background color as transparent
                image.Trim(); //cut off all transparancy

                image.Write(codex.CoverArt);
                CoverFetcher.CreateThumbnail(codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to extract Cover from pdf.", ex);
                return false;
            }
        }

        public override bool IsValidSource(Codex codex) => File.Exists(codex.Path) && Path.GetExtension(codex.Path) == ".pdf";
    }
}
