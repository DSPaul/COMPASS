using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using ImageMagick;
using ImageMagick.Formats;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace COMPASS.Common.Sources
{
    public class PdfMetaDataSource : MetaDataSource
    {
        public PdfMetaDataSource(CodexCollection targetCollection) :  
            base(targetCollection) { }
        
        public override MetaDataSourceType Type => MetaDataSourceType.PDF;
        public override bool IsValidSource(SourceSet sources) => IOService.IsPDFFile(sources.Path);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Codex without pdf found in pdf source");
            PdfDocument? pdfDoc = null;
            
            SourceMetaData metaData = new();
            try
            {
                PdfDocumentInfo? info = await Task.Run(() =>
                {
                    PdfReader pdfReader = new(sources.Path);
                    pdfDoc = new PdfDocument(pdfReader);
                    return pdfDoc.GetDocumentInfo();
                });

                metaData.Title = info.GetTitle() ?? string.Empty;
                if (info.GetAuthor() is not null)
                {
                    metaData.Authors = [info.GetAuthor()];
                }
                metaData.PageCount = pdfDoc!.GetNumberOfPages();

                // If it already has an ISBN, no need to check again
                if (!string.IsNullOrEmpty(sources.ISBN)) return metaData;

                //Search for an ISBN in first 5 pages
                for (int page = 1; page <= Math.Min(5, pdfDoc.GetNumberOfPages()); page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                    //strip text of spaces
                    pageContent = Constants.RegexWhitespace().Replace(pageContent, "");
                    //search ISBN
                    string isbn = Constants.RegexISBN().Match(pageContent).Value;
                    if (!string.IsNullOrEmpty(isbn))
                    {
                        sources.ISBN = isbn;
                        break;
                    }
                }
            }

            catch (Exception ex)
            {
                //in case pdf is corrupt: PdfReader will throw error
                //in those cases: import the pdf without opening it
                Logger.Error($"Failed to read metadata from {Path.GetFileName(sources.Path)}", ex);
                LogEntry logEntry = new(Severity.Warning, $"Failed to read metadata from {metaData.Title}");
                ProgressVM.AddLogEntry(logEntry);
            }

            finally { pdfDoc?.Close(); }
            
            return metaData;
        }

        public override async Task<IMagickImage?> FetchCover(SourceSet sources)
        {
            //return false if the file doesn't exist
            if (!IOService.IsPDFFile(sources.Path) ||
                !File.Exists(sources.Path))
            {
                return null;
            }

            try //image.Read can throw exception if the file can not be opened/read
            {
                MagickImage image = new();
                await image.ReadAsync(sources.Path, ReadSettings);
                image.Format = MagickFormat.Png;

                //some pdf's are transparent, expecting a white page underneath
                image.BackgroundColor = new MagickColor("#FFFFFF");
                image.Alpha(AlphaOption.Remove);
                return image;
            }
            catch (Exception ex)
            {
                string logMsg = $"Failed to generate cover from {Path.GetFileName(sources.Path)}";
                Logger.Error(logMsg, ex);
                LogEntry logEntry = new(Severity.Warning, logMsg);
                ProgressVM.AddLogEntry(logEntry);
                return null;
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
