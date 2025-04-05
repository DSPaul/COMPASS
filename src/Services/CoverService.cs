﻿using COMPASS.Models;
using COMPASS.Models.CodexProperties;
using COMPASS.Tools;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Sources;
using COMPASS.Windows;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Services
{
    public static class CoverService
    {
        /// <summary>
        /// Fethces a cover image for the given codex
        /// </summary>
        /// <param name="codex"></param>
        /// <param name="chooseMetaDataViewModel"></param>
        /// <exception cref="System.OperationCanceledException">The token has had cancellation requested.</exception>
        public static async Task GetCover(Codex codex, ChooseMetaDataViewModel? chooseMetaDataViewModel = null)
        {
            Codex MetaDatalessCodex = new()
            {
                Sources = codex.Sources,
                ID = codex.ID,
            };

            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.CodexProperties.First(prop => prop.Name == nameof(Codex.CoverArt));

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

                SourceViewModel? sourceVM = SourceViewModel.GetSourceVM(source);
                if (sourceVM == null || !sourceVM.IsValidSource(codex.Sources)) continue;
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
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1)
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

        public static async Task SaveCover(Codex destCodex, IMagickImage image)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArt))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            if (image.Width > 850) image.Resize(850, 0);

            if (IOService.EnsureFoldersExists(destCodex.CoverArt))
            {
                await image.WriteAsync(destCodex.CoverArt);
                CreateThumbnail(destCodex, image);
            }
        }

        public static async Task SaveCover(string imgURL, Codex destCodex)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArt))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            var imgBytes = await IOService.DownloadFileAsync(imgURL);
            if (IOService.EnsureFoldersExists(destCodex.CoverArt))
            {
                await File.WriteAllBytesAsync(destCodex.CoverArt, imgBytes);
                CreateThumbnail(destCodex);
                destCodex.RefreshThumbnail();
            }
        }

        public static bool GetCoverFromImage(string imagePath, Codex destCodex)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArt))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return false;
            }

            //check if it's a valid file
            if (String.IsNullOrEmpty(imagePath) ||
                !Path.Exists(imagePath) ||
                !IOService.IsImageFile(imagePath))
            {
                return false;
            }

            try
            {
                if (IOService.EnsureFoldersExists(destCodex.CoverArt))
                {
                    using (MagickImage image = new(imagePath))
                    {
                        if (image.Width > 1000) image.Resize(1000, 0);
                        image.Write(destCodex.CoverArt);
                        CreateThumbnail(destCodex, image);
                    }
                    destCodex.RefreshThumbnail();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                Logger.Error($"Failed to generate a thumbnail for {imagePath}", ex);
                return false;
            }
        }

        public static void CreateThumbnail(Codex c, IMagickImage? image = null)
        {
            if (String.IsNullOrEmpty(c.Thumbnail))
            {
                Logger.Error("Trying to write thumbnail to empty path", new InvalidOperationException());
                return;
            }

            uint newWidth = 200; //sets resolution of thumbnail in pixels
            bool ownsImage = false;

            if (image is null)
            {
                if (!Path.Exists(c.CoverArt)) return;
                image = new MagickImage(c.CoverArt);
                ownsImage = true;
            }

            try
            {
                //preserve aspect ratio
                uint width = image.Width;
                uint height = image.Height;
                uint newHeight = newWidth / width * height;
                //create thumbnail
                if (IOService.EnsureFoldersExists(c.Thumbnail))
                {
                    image.Thumbnail(newWidth, newHeight);
                    image.Write(c.Thumbnail);
                }
            }
            finally
            {
                if (ownsImage)
                {
                    image?.Dispose();
                }
            }
        }

        //Take screenshot of specific html element 
        public static IMagickImage GetCroppedScreenShot(IWebDriver driver, IWebElement webElement)
            => GetCroppedScreenShot(driver, webElement.Location, webElement.Size);

        public static IMagickImage GetCroppedScreenShot(IWebDriver driver, Point location, Size size)
        {
            //take the screenshot
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            var mf = new MagickImageFactory();
            using var img = mf.Create(ss.AsByteArray);
            img.Resize(3000, 3000); //same size as headless window
            return img.CloneArea(location.X, location.Y, (uint)size.Width, (uint)size.Height);
        }
    }
}
