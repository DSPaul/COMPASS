using System.Diagnostics;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using ImageMagick;
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
        public override bool IsValidSource(SourceSet sources) => FileFormatUtils.IsPDFFile(sources.Path);

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
                    pageContent = RegexConstants.Whitespace().Replace(pageContent, "");
                    //search ISBN
                    string isbn = RegexConstants.ISBN().Match(pageContent).Value;
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

        public override Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            //return false if the file doesn't exist
            if (!FileFormatUtils.IsPDFFile(sources.Path) ||
                !File.Exists(sources.Path))
            {
                return Task.FromResult<IMagickImage<byte>?>(null);
            }
            
            try //reading an image can throw exception if file can not be opened/read
            {
                using var pdfStream = new FileStream(sources.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var imageStream = new MemoryStream();
#pragma warning disable CA1416
                PDFtoImage.Conversion.SavePng(imageStream, pdfStream, options: ReadOptions);
#pragma warning restore CA1416
                imageStream.Position = 0;
                return Task.FromResult<IMagickImage<byte>?>(new MagickImage(imageStream));
            }
            catch (Exception ex)
            {
                string logMsg = $"Failed to generate cover from {Path.GetFileName(sources.Path)}";
                Logger.Error(logMsg, ex);
                LogEntry logEntry = new(Severity.Warning, logMsg);
                ProgressVM.AddLogEntry(logEntry);
                return Task.FromResult<IMagickImage<byte>?>(null);
            }
        }
        
        private static PDFtoImage.RenderOptions? _readOptions;
        private static PDFtoImage.RenderOptions ReadOptions => _readOptions ??= 
            new PDFtoImage.RenderOptions(
                BackgroundColor: SkiaSharp.SKColor.Parse("#FFFFFF"), //some pdf's are transparent, expecting a white page underneath
                Width: 850, 
                WithAspectRatio: true);
    }
}
