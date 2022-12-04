using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS
{
    public static class CoverFetcher
    { 
        public static bool GetCover(Codex c)
        {
            bool success = Utils.TryFunctions(GetCoverFunctions,c);
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

                    try
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
        public static bool GetCoverFromURL(Codex c)
        {
            string URL = c.SourceURL;
            Enums.Sources source;
            if (String.IsNullOrEmpty(URL)) return false;

            if (URL.Contains("dndbeyond.com")) source = Enums.Sources.DnDBeyond;
            else if (URL.Contains("gmbinder.com")) source = Enums.Sources.GmBinder;
            else if (URL.Contains("homebrewery.naturalcrit.com")) source = Enums.Sources.Homebrewery;
            else if (URL.Contains("drive.google.com")) source = Enums.Sources.GoogleDrive;
            //if none of above, unsupported site
            else
            {
                Uri tempUri = new(URL);
                string message = tempUri.Host + " is not a supported source at the moment.";
                MessageBox.Show(message, "Cover could not be found.", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return GetCoverFromURL(c, source);
        }

        public static bool GetCoverFromURL(Codex destfile, Enums.Sources source)
        {
            string URL = destfile.SourceURL;

            //sites that store cover as image that can be downloaded
            if(source.HasFlag(Enums.Sources.DnDBeyond) || source.HasFlag(Enums.Sources.GoogleDrive) || source.HasFlag(Enums.Sources.ISBN))
            {
                try
                {
                    HtmlWeb web = new();
                    HtmlDocument doc;
                    HtmlNode src;
                    string imgURL = "";

                    switch (source)
                    {
                        case Enums.Sources.DnDBeyond:
                            //cover art is on store page, redirect there by going to /credits which every book has
                            doc = web.Load(string.Concat(URL, "/credits"));
                            src = doc.DocumentNode;

                            imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']").GetAttributeValue("content", String.Empty);
                            break;

                        case Enums.Sources.GoogleDrive:
                            doc = web.Load(URL);
                            src = doc.DocumentNode;

                            imgURL = src.SelectSingleNode("//meta[@property='og:image']").GetAttributeValue("content", String.Empty);
                            //cut of "=W***-h***-p" from URL that crops the image if it is present
                            if (imgURL.Contains('=')) imgURL = imgURL.Split('=')[0];
                            break;
                        case Enums.Sources.ISBN:
                            string uri = $"https://openlibrary.org/isbn/{destfile.ISBN}.json";
                            JObject metadata = Task.Run(async () => await Utils.GetJsonAsync(uri)).Result;
                            string imgID = (string)metadata.SelectToken("covers[0]");
                            imgURL = $"https://covers.openlibrary.org/b/id/{imgID}.jpg";
                            break;
                    }
                    //download the file
                    var imgBytes = Task.Run(async () => await Utils.DownloadFileAsync(imgURL)).Result;
                    File.WriteAllBytes(destfile.CoverArt, imgBytes);
                }
                catch(Exception ex)
                {
                    Logger.log.Error(ex.InnerException);
                    return false;
                }
            }
            
            //sites do not store cover as img, Use Selenium for screenshotting pages
            else if (source.HasFlag(Enums.Sources.GmBinder) || source.HasFlag(Enums.Sources.Homebrewery))
            {
                var browser = (Enums.Browser)Properties.Settings.Default.SeleniumBrowser;

                string driverPath = browser switch
                {
                    Enums.Browser.Chrome => Utils.FindFileDirectory("chromedriver.exe", Constants.WebDriverDirectoryPath),
                    Enums.Browser.Firefox => Utils.FindFileDirectory("geckodriver.exe", Constants.WebDriverDirectoryPath),
                    _ => Utils.FindFileDirectory("msedgedriver.exe", Constants.WebDriverDirectoryPath)
                };

                DriverService driverService = browser switch
                {
                    Enums.Browser.Chrome => ChromeDriverService.CreateDefaultService(driverPath),
                    Enums.Browser.Firefox => FirefoxDriverService.CreateDefaultService(driverPath),
                    _ => EdgeDriverService.CreateDefaultService(driverPath)
                };

                driverService.HideCommandPromptWindow = true;

                List<string> DriverArguments = new()
                {
                    "--headless",
                    "--window-size=3000,3000",
                    "--width=3000",
                    "--height=3000"
                };

                WebDriver driver;
                switch (browser)
                {
                    case Enums.Browser.Chrome:
                        ChromeOptions CO = new();
                        CO.AddArguments(DriverArguments);
                        driver = new ChromeDriver((ChromeDriverService)driverService, CO);
                        break;

                    case Enums.Browser.Firefox:
                        FirefoxOptions FO = new();
                        FO.AddArguments(DriverArguments);
                        driver = new FirefoxDriver((FirefoxDriverService)driverService, FO);
                        break;

                    default:
                        EdgeOptions EO = new();
                        EO.AddArguments(DriverArguments);
                        driver = new EdgeDriver((EdgeDriverService)driverService, EO);
                        break;
                }

                IWebElement Coverpage;
                MagickImage image = null;
                try
                {
                    driver.Navigate().GoToUrl(URL);

                    switch (source)
                    {
                        case Enums.Sources.GmBinder:
                            Coverpage = driver.FindElement(By.Id("p1"));
                            //screenshot and download the image
                            image = GetCroppedScreenShot(driver, Coverpage.Location, Coverpage.Size);
                            break;

                        case Enums.Sources.Homebrewery:
                            //get nav height because scraper doesn't see nav anymore after it switched to frame
                            var nav = driver.FindElement(By.XPath("//nav"));
                            string navhstr = nav.GetCssValue("height");
                            float navheight = float.Parse(navhstr[..(navhstr.Length - 2)], CultureInfo.InvariantCulture);

                            //switch to iframe
                            var iframe = driver.FindElement(By.XPath("//iframe"));
                            driver.SwitchTo().Frame(iframe);
                            Coverpage = driver.FindElement(By.Id("p1"));
                            //shift page down by nav height because element location is relative to frame, but coords on screenshot is reletaive to page 
                            int newY = (int)Math.Round(Coverpage.Location.Y + navheight, 0);

                            //screenshot and download the image
                            image = GetCroppedScreenShot(driver, new System.Drawing.Point(Coverpage.Location.X, newY), Coverpage.Size);
                            break;
                    }

                    if (image.Width > 850) image.Resize(850, 0);
                    image.Write(destfile.CoverArt);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex.InnerException);
                    return false;
                }
                finally
                {
                    driver.Quit();
                }
            }
            
            CreateThumbnail(destfile);
            return true;
        }
        
        public static bool GetCoverFromISBN(Codex c)
        {
            if (string.IsNullOrEmpty(c.ISBN)) return false;
            return GetCoverFromURL(c,Enums.Sources.ISBN);
        }

        //get cover from image
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
                Logger.log.Error(String.Format("Could not get cover from {0}", imagepath));
                Logger.log.Error(ex.InnerException);
            }
        }

        //create thumbnail from cover
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
