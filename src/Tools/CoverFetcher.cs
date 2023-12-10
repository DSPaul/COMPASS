using COMPASS.Models;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Sources;
using COMPASS.Windows;
using ImageMagick;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static class CoverFetcher
    {
        public static async Task GetCover(Codex codex, ChooseMetaDataViewModel chooseMetaDataViewModel = null)
        {
            Codex MetaDatalessCodex = new()
            {
                Path = codex.Path,
                SourceURL = codex.SourceURL,
                ISBN = codex.ISBN,
                ID = codex.ID,
            };

            CodexProperty coverProp = SettingsViewModel.GetInstance().MetaDataPreferences.First(prop => prop.Label == "Cover Art");

            if (coverProp.OverwriteMode == MetaDataOverwriteMode.Ask)
            {
                Debug.Assert(chooseMetaDataViewModel is not null, "choose MetaData ViewModel cannot be null if overwrite mode is ask");
            }

            if (coverProp.OverwriteMode == MetaDataOverwriteMode.Never ||
                (coverProp.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !coverProp.IsEmpty(codex)))
            {
                ProgressViewModel.GetInstance().IncrementCounter();
                return;
            }

            //copy img paths over this way
            MetaDatalessCodex.SetImagePaths(MainViewModel.CollectionVM.CurrentCollection);

            bool shouldAsk = coverProp.OverwriteMode == MetaDataOverwriteMode.Ask && !coverProp.IsEmpty(codex);
            if (shouldAsk)
            {
                //set img paths to temp path
                MetaDatalessCodex.CoverArt = codex.CoverArt.Insert(codex.CoverArt.Length - 4, ".tmp");
                MetaDatalessCodex.Thumbnail = codex.Thumbnail.Insert(codex.Thumbnail.Length - 4, ".tmp");
            }

            bool getCoverSuccessful = false;
            foreach (var source in coverProp.SourcePriority)
            {
                ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                SourceViewModel sourceVM = SourceViewModel.GetSourceVM(source);
                if (sourceVM == null || !sourceVM.IsValidSource(codex)) continue;
                getCoverSuccessful = await sourceVM.FetchCover(MetaDatalessCodex);
                if (getCoverSuccessful) break;
            }

            if (shouldAsk)
            {
                //check if the image is different from the existing one
                using MagickImage origCover = new(codex.CoverArt);
                using MagickImage newCover = new(MetaDatalessCodex.CoverArt);
                var isEqual = origCover.Compare(newCover).MeanErrorPerPixel == 0;
                if (!isEqual)
                {
                    chooseMetaDataViewModel?.AddCodexPair(codex, MetaDatalessCodex);
                }
            }

            if (getCoverSuccessful) codex.RefreshThumbnail();
            ProgressViewModel.GetInstance().IncrementCounter();
        }

        public static async Task GetCover(List<Codex> codices)
        {
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.ResetCounter();
            progressVM.TotalAmount = codices.Count;
            progressVM.Text = "Getting Cover";

            ChooseMetaDataViewModel chooseMetaDataVM = new();

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            };

            try
            {
                await Parallel.ForEachAsync(codices, parallelOptions, async (codex, _) => await GetCover(codex, chooseMetaDataVM));
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Renewing covers has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }

            if (chooseMetaDataVM.CodicesWithChoices.Any())
            {
                ChooseMetaDataWindow window = new(chooseMetaDataVM);
                window.Show();
            }
        }

        public static void SaveCover(MagickImage image, Codex destCodex)
        {
            if (image.Width > 850) image.Resize(850, 0);
            image.Write(destCodex.CoverArt);
            CreateThumbnail(destCodex);
        }

        public static async Task SaveCover(string imgURL, Codex destCodex)
        {
            var imgBytes = await Utils.DownloadFileAsync(imgURL);
            await File.WriteAllBytesAsync(destCodex.CoverArt, imgBytes);
            CreateThumbnail(destCodex);
        }

        public static bool GetCoverFromImage(string imagePath, Codex destFile)
        {
            //check if it's a valid file
            if (String.IsNullOrEmpty(imagePath) ||
                !Path.Exists(imagePath) ||
                !Utils.IsImageFile(imagePath))
            {
                return false;
            }

            try
            {
                using MagickImage image = new(imagePath);
                if (image.Width > 1000) image.Resize(1000, 0);
                image.Write(destFile.CoverArt);
                CreateThumbnail(destFile);
                return true;
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                Logger.Error($"Failed to generate a thumbnail for {imagePath}", ex);
                return false;
            }
        }

        public static void CreateThumbnail(Codex c)
        {
            int newWidth = 200; //sets resolution of thumbnail in pixels
            if (!Path.Exists(c.CoverArt)) return;
            using MagickImage image = new(c.CoverArt);
            //preserve aspect ratio
            int width = image.Width;
            int height = image.Height;
            int newHeight = newWidth / width * height;
            //create thumbnail
            image.Thumbnail(newWidth, newHeight);
            image.Write(c.Thumbnail);
            c.RefreshThumbnail();
        }

        //Take screenshot of specific html element 
        public static MagickImage GetCroppedScreenShot(IWebDriver driver, IWebElement webElement)
            => GetCroppedScreenShot(driver, webElement.Location, webElement.Size);

        private static MagickImage GetCroppedScreenShot(IWebDriver driver, Point location, Size size)
        {
            //take the screenshot
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            var img = Image.FromStream(new MemoryStream(ss.AsByteArray)) as Bitmap;
            if (img == null) return new MagickImage();
            var imgCropped = img.Clone(new Rectangle(location, size), img.PixelFormat);
            var mf = new MagickFactory();
            return new(mf.Image.Create(imgCropped));
        }
    }
}
