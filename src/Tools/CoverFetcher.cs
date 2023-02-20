using COMPASS.Models;
using COMPASS.ViewModels.Sources;
using ImageMagick;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.Tools
{
    public static class CoverFetcher
    {
        public static bool GetCover(Codex codex)
        {
            bool success = PreferableFunction<Codex>.TryFunctions(GetCoverFunctions, codex);
            if (!success) MessageBox.Show("Could not get Cover, please check local path or URL");
            return success;
        }

        //list with possible functions to get Cover
        private static readonly List<PreferableFunction<Codex>> GetCoverFunctions = new()
            {
                new PreferableFunction<Codex>("Local File", GetCoverFromFile,0),
                new PreferableFunction<Codex>("Web Version", GetCoverFromURL,1),
                new PreferableFunction<Codex>("ISBN", GetCoverFromISBN,2)
            };

        //Save First page of PDF as png
        public static bool GetCoverFromFile(Codex codex)
        {
            //return false if file doesn't exist
            if (String.IsNullOrEmpty(codex.Path) || !File.Exists(codex.Path)) return false;

            string FileType = System.IO.Path.GetExtension(codex.Path);
            switch (FileType)
            {
                case ".pdf":
                    var pdfReadDefines = new ImageMagick.Formats.PdfReadDefines()
                    {
                        HideAnnotations = true,
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
                        using (MagickImage image = new())
                        {
                            image.Read(codex.Path, settings);
                            image.Format = MagickFormat.Png;
                            image.BackgroundColor = new MagickColor("#000000"); //set background color as transparant
                            image.Border(20); //adds transparant border around image
                            image.Trim(); //cut off all transparancy
                            image.RePage(); //resize image to fit what was cropped

                            image.Write(codex.CoverArt);
                            CreateThumbnail(codex);
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error(ex.InnerException);
                        string messageBoxText = "Failed to extract Cover from pdf.";
                        MessageBox.Show(messageBoxText, "Failed to extract Cover from pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".webp":
                    GetCoverFromImage(codex.Path, codex);
                    return true;

                default:
                    return false;
            }

        }

        //Get cover from URL
        public static bool GetCoverFromURL(Codex codex)
        {
            string URL = codex.SourceURL;
            if (String.IsNullOrEmpty(URL)) return false;

            OnlineSourceViewModel sourceViewModel; ;
            if (URL.Contains("dndbeyond.com")) sourceViewModel = new DndBeyondSourceViewModel();
            else if (URL.Contains("gmbinder.com")) sourceViewModel = new GmBinderSourceViewModel();
            else if (URL.Contains("homebrewery.naturalcrit.com")) sourceViewModel = new HomebrewerySourceViewModel();
            else if (URL.Contains("drive.google.com")) sourceViewModel = new GoogleDriveSourceViewModel();
            //if none of above, unsupported site
            else
            {
                Uri tempUri = new(URL);
                string message = tempUri.Host + " is not a supported source at the moment.";
                MessageBox.Show(message, "Cover could not be found.", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return sourceViewModel.FetchCover(codex);
        }

        public static bool GetCoverFromISBN(Codex codex)
        {
            if (string.IsNullOrEmpty(codex.ISBN)) return false;
            return new ISBNSourceViewModel().FetchCover(codex);
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

        public static void GetCoverFromImage(string imagepath, Codex destfile)
        {
            try
            {
                using MagickImage image = new(imagepath);
                if (image.Width > 1000) image.Resize(1000, 0);
                image.Write(destfile.CoverArt);
                CreateThumbnail(destfile);
            }
            catch (Exception ex)
            {
                //will fail if image is corrupt
                Logger.log.Error($"Could not get cover from {imagepath}");
                Logger.log.Error(ex.InnerException);
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
