﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Sources;
using COMPASS.Common.Views.Windows;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;

namespace COMPASS.Common.Services
{
    public static class CoverService
    {
        /// <summary>
        /// Fetches a cover image for the given codex
        /// </summary>
        /// <param name="codex"></param>
        /// <param name="chooseMetaDataViewModel"></param>
        /// <exception cref="System.OperationCanceledException">The token has had cancellation requested.</exception>
        public static async Task GetCover(Codex codex, ChooseMetaDataViewModel? chooseMetaDataViewModel = null)
        {
            var thumbnailStorageService = App.Container.Resolve<IThumbnailStorageService>();
            
            Codex metaDatalessCodex = new(codex.Collection)
            {
                Sources = codex.Sources,
                ID = codex.ID,
            };

            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.CodexProperties.First(prop => prop.Name == nameof(Codex.CoverArtPath));

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

            //set img paths
            thumbnailStorageService.InitCodexImagePaths(metaDatalessCodex);

            bool shouldAsk = coverProp.OverwriteMode == MetaDataOverwriteMode.Ask && !coverProp.IsEmpty(codex);
            if (shouldAsk)
            {
                //set img paths to temp path
                metaDatalessCodex.CoverArtPath = codex.CoverArtPath.Insert(codex.CoverArtPath.Length - 4, ".tmp");
                metaDatalessCodex.ThumbnailPath = codex.ThumbnailPath.Insert(codex.ThumbnailPath.Length - 4, ".tmp");
            }

            bool getCoverSuccessful = false;
            foreach (var source in coverProp.SourcePriority)
            {
                ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                SourceViewModel? sourceVm = SourceViewModel.GetSourceVM(source);
                if (sourceVm == null || !sourceVm.IsValidSource(codex.Sources)) continue;
                getCoverSuccessful = await sourceVm.FetchCover(metaDatalessCodex);
                if (getCoverSuccessful) break;
            }

            if (shouldAsk)
            {
                //check if the image is different from the existing one
                using MagickImage origCover = new(codex.CoverArtPath);
                using MagickImage newCover = new(metaDatalessCodex.CoverArtPath);
                var isEqual = origCover.Compare(newCover).MeanErrorPerPixel == 0;
                if (!isEqual)
                {
                    chooseMetaDataViewModel?.AddCodexPair(codex, metaDatalessCodex);
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

        public static void SaveCover(Codex destCodex, IMagickImage image)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            if (image.Width > 850) image.Resize(850, 0);

            image.Write(destCodex.CoverArtPath);
            CreateThumbnail(destCodex, image);
        }

        public static async Task SaveCover(string imgURL, Codex destCodex)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArtPath))
            {
                Logger.Error("Trying to write cover img to empty path", new InvalidOperationException());
                return;
            }

            var imgBytes = await IOService.DownloadFileAsync(imgURL);
            await File.WriteAllBytesAsync(destCodex.CoverArtPath, imgBytes);
            CreateThumbnail(destCodex);
            destCodex.RefreshThumbnail();
        }

        public static bool GetCoverFromImage(string imagePath, Codex destCodex)
        {
            if (String.IsNullOrEmpty(destCodex.CoverArtPath))
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
                using (MagickImage image = new(imagePath))
                {
                    if (image.Width > 1000) image.Resize(1000, 0);
                    image.Write(destCodex.CoverArtPath);
                    CreateThumbnail(destCodex, image);
                }
                destCodex.RefreshThumbnail();
                return true;
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
            if (String.IsNullOrEmpty(c.ThumbnailPath))
            {
                Logger.Error("Trying to write thumbnail to empty path", new InvalidOperationException());
                return;
            }

            uint newWidth = 200; //sets resolution of thumbnail in pixels
            bool ownsImage = false;

            if (image is null)
            {
                if (!Path.Exists(c.CoverArtPath)) return;
                image = new MagickImage(c.CoverArtPath);
                ownsImage = true;
            }

            try
            {
                //preserve aspect ratio
                uint width = image.Width;
                uint height = image.Height;
                uint newHeight = newWidth / width * height;
                //create thumbnail
                image.Thumbnail(newWidth, newHeight);
                image.Write(c.ThumbnailPath);
            }
            finally
            {
                if (ownsImage)
                {
                    image.Dispose();
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
