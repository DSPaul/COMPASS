using COMPASS.Models;
using COMPASS.ViewModels;
using COMPASS.ViewModels.Sources;
using ImageMagick;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static class CoverFetcher
    {
        public static async Task GetCover(Codex codex)
        {
            Codex MetaDatalessCodex = new()
            {
                Path = codex.Path,
                SourceURL = codex.SourceURL,
                ISBN = codex.ISBN,
                ID = codex.ID,
            };

            MetaDatalessCodex.SetImagePaths(MainViewModel.CollectionVM.CurrentCollection);

            CodexProperty coverProp = SettingsViewModel.GetInstance().MetaDataPreferences.First(prop => prop.Label == "Cover Art");

            if (coverProp.OverwriteMode == MetaDataOverwriteMode.Never ||
                (coverProp.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !coverProp.IsEmpty(codex)))
                return;

            bool getCoverSuccesfull = false;
            foreach (var source in coverProp.SourcePriority)
            {
                SourceViewModel sourceVM = SourceViewModel.GetSourceVM(source);
                getCoverSuccesfull = await sourceVM.FetchCover(MetaDatalessCodex);
                if (getCoverSuccesfull) break;
            }

            codex.RefreshThumbnail();
            ProgressViewModel.GetInstance().IncrementCounter();
        }

        public static async Task GetCover(List<Codex> codices)
        {
            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.ResetCounter();
            ProgressVM.TotalAmount = codices.Count;
            ProgressVM.Text = "Getting Cover";

            await Task.Run(() => Parallel.ForEach(codices, codex => GetCover(codex)));
        }

        public static void SaveCover(MagickImage image, Codex destCodex)
        {
            if (image.Width > 850) image.Resize(850, 0);
            image.Write(destCodex.CoverArt);
            CreateThumbnail(destCodex);
        }

        public static void SaveCover(string imgURL, Codex destCodex)
        {
            var imgBytes = Task.Run(async () => await Utils.DownloadFileAsync(imgURL)).Result;
            File.WriteAllBytes(destCodex.CoverArt, imgBytes);
            CreateThumbnail(destCodex);
        }

        public static bool GetCoverFromImage(string imagepath, Codex destfile)
        {
            //check if it's a valid file
            if (string.IsNullOrEmpty(imagepath) || !Path.Exists(imagepath)) return false;

            //check if it's a valid image format
            string ext = Path.GetExtension(imagepath);
            if (ext != ".png" &&
                ext != ".jpg" &&
                ext != ".jpeg" &&
                ext != ".webp") return false;

            try
            {
                using MagickImage image = new(imagepath);
                if (image.Width > 1000) image.Resize(1000, 0);
                image.Write(destfile.CoverArt);
                CreateThumbnail(destfile);
                return true;
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                Logger.Error($"Could not get cover from {imagepath}", ex);
                return false;
            }
        }

        public static void CreateThumbnail(Codex c)
        {
            int newwidth = 200; //sets resolution of thumbnail in pixels
            using MagickImage image = new(c.CoverArt);
            //preserve aspect ratio
            int width = image.Width;
            int height = image.Height;
            int newheight = newwidth / width * height;
            //create thumbnail
            image.Thumbnail(newwidth, newheight);
            image.Write(c.Thumbnail);
            c.RefreshThumbnail();
        }

        //Take screenshot of specific html element 
        public static MagickImage GetCroppedScreenShot(IWebDriver driver, IWebElement webElement)
            => GetCroppedScreenShot(driver, webElement.Location, webElement.Size);

        public static MagickImage GetCroppedScreenShot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size)
        {
            //take the screenshot
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            var img = Image.FromStream(new MemoryStream(ss.AsByteArray)) as Bitmap;

            var imgcropped = img.Clone(new Rectangle(location, size), img.PixelFormat);
            var mf = new MagickFactory();
            MagickImage Magickimg = new(mf.Image.Create(imgcropped));
            return Magickimg;
        }
    }
}
