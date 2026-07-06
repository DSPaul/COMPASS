using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using ImageMagick;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Logging;

namespace COMPASS.Common.Sources
{
    public class PdfMetaDataSource : MetaDataSource
    {
        public PdfMetaDataSource(CodexCollection targetCollection) :
            base(targetCollection)
        { }

        public override MetaDataSourceType Type => MetaDataSourceType.PDF;
        public override bool IsValidSource(SourceSet sources) => FileFormatUtils.IsPDFFile(sources.Path);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Codex without pdf found in pdf source");

            SourceMetaData metaData = new();
            try
            {
                await Task.Run(() =>
                {
                    //using filestream is way more perfomant than calling PdfDocument.Open() directly with the path which would read the entire pdf immediatly
                    using var fileStream = new FileStream(sources.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                  using PdfDocument pdfDoc = PdfDocument.Open(fileStream, new ParsingOptions()
                    {
                        Logger = new PdfLogger()
                    });

                    metaData.Title = pdfDoc.Information.Title ?? string.Empty;
                    if (pdfDoc.Information.Author is not null)
                    {
                        metaData.Authors = [pdfDoc.Information.Author];
                    }
                    metaData.PageCount = pdfDoc.NumberOfPages;

                    // If it already has an ISBN, no need to check again
                    if (!string.IsNullOrEmpty(sources.ISBN)) return;

                    //Search for an ISBN in first 5 pages
                    for (int pageNum = 1; pageNum <= Math.Min(5, pdfDoc.NumberOfPages); pageNum++)
                    {
                        Page page = pdfDoc.GetPage(pageNum);
                        //strip text of spaces
                        string pageContent = RegexConstants.Whitespace().Replace(ContentOrderTextExtractor.GetText(page), "");
                        //search ISBN
                        string isbn = RegexConstants.ISBN().Match(pageContent).Value;
                        if (!string.IsNullOrEmpty(isbn))
                        {
                            sources.ISBN = isbn;
                            break;
                        }
                    }
                });
            }

            catch (Exception ex)
            {
                //in case pdf is corrupt: PdfDocument.Open will throw error
                //in those cases: import the pdf without opening it
                Logger.Error($"Failed to read metadata from {Path.GetFileName(sources.Path)}", ex);
                LogEntry logEntry = new(Severity.Warning, $"Failed to read metadata from {metaData.Title}");
                ProgressVM.AddLogEntry(logEntry);
            }

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

        private class PdfLogger : ILog
        {
            ILogger logger = ServiceResolver.Resolve<ILogger>();

            public void Debug(string message) { } //Too much stuff I don't care about, don't log it
            public void Debug(string message, Exception ex) => logger.Debug(message, ex);
            public void Warn(string message) => logger.Warn(message);
            public void Error(string message) => logger.Error(message, new Exception());
            public void Error(string message, Exception ex) => logger.Error(message, ex);
        }
    }
}
