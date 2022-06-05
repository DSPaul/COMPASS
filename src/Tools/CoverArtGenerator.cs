using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using ImageMagick;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;

namespace COMPASS
{
    public static class CoverArtGenerator
    { 
        //Save First page of PDF as png
        public static void GetCoverFromPDF(Codex pdf)
        {

            var pdfReadDefines = new ImageMagick.Formats.PdfReadDefines()
            {
                HideAnnotations = true,
            };

            MagickReadSettings settings = new MagickReadSettings()
            {
                Density = new Density(100,100),
                FrameIndex = 0, // First page
                FrameCount = 1, // Number of pages
                Defines = pdfReadDefines,
            };

            try
            {
                using (MagickImage image = new MagickImage())
                {
                    image.Read(pdf.Path, settings);
                    image.Format = MagickFormat.Png;
                    image.BackgroundColor = new MagickColor("#000000"); //set backgroun color as transparant
                    image.Border(20); //adds transparant border around image
                    image.Trim(); //cut off all transparancy
                    image.RePage(); //resize image to fit what was cropped

                    image.Write(pdf.CoverArt);
                    CreateThumbnail(pdf);
                }
            }
            catch
            {
                string messageBoxText = "COMPASS uses Ghostscript to extract cover art from pdf's. \n" +
                    "Ghostscript needs to be installed seperatly and can be downloaded here: \n" +
                    "https://www.ghostscript.com/releases/gsdnld.html";
                MessageBox.Show(messageBoxText,"Could not extract cover art from pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Get cover from URL
        public static void GetCoverFromURL(string URL, Codex destfile, Enums.ImportMode import)
        {
            if (import == Enums.ImportMode.DnDBeyond)
            {
                HtmlWeb web = new HtmlWeb();
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = web.Load(string.Concat(URL, "/credits"));
                HtmlNode src = doc.DocumentNode;

                string imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']").GetAttributeValue("content", String.Empty);

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(imgURL), destfile.CoverArt);
                }
            }

            //Use Selenium for screenshotting pages
            DriverService driverService;
            WebDriver driver;
            switch (Properties.Settings.Default.SeleniumBrowser)
            {
                case (int)Enums.Browser.Chrome:
                    driverService = ChromeDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    ChromeOptions CO = new ChromeOptions();
                    CO.AddArgument("--window-size=2500,2000");
                    CO.AddArgument("--headless");
                    driver = new ChromeDriver((ChromeDriverService)driverService, CO);
                    break;

                case (int)Enums.Browser.Firefox:
                    driverService = FirefoxDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    FirefoxOptions FO = new FirefoxOptions();
                    FO.AddArgument("--window-size=2500,2000");
                    FO.AddArgument("--headless");
                    driver = new FirefoxDriver((FirefoxDriverService)driverService, FO);
                    break;

                default:
                    driverService = EdgeDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    EdgeOptions EO = new EdgeOptions();
                    EO.AddArgument("--window-size=2500,2000");
                    EO.AddArgument("--headless");
                    driver = new EdgeDriver((EdgeDriverService)driverService, EO);
                    break;
            }

            IWebElement Coverpage;
            MagickImage image = null;
            try
            {
                driver.Navigate().GoToUrl(URL);

                switch (import)
                {
                    case Enums.ImportMode.GmBinder:
                        Coverpage = driver.FindElement(By.Id("p1"));
                        //screenshot and download the image
                        image = GetCroppedScreenShot(driver, Coverpage.Location, Coverpage.Size);
                        break;

                    case Enums.ImportMode.Homebrewery:
                        //get nav height because scraper doesn't see nav anymore after it switched to frame
                        var nav = driver.FindElement(By.XPath("//nav"));
                        string navhstr = nav.GetCssValue("height");
                        float navheight = float.Parse(navhstr.Substring(0, navhstr.Length - 2), CultureInfo.InvariantCulture);

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
            catch 
            {
                driver.Quit();
            }

            driver.Quit();
            CreateThumbnail(destfile);
        }

        //get cover from image
        public static void GetCoverFromImage(string imagepath, Codex destfile)
        {
            using (MagickImage image = new MagickImage(imagepath))
            {
                if (image.Width > 1000) image.Resize(1000, 0);
                image.Write(destfile.CoverArt);
                CreateThumbnail(destfile);
            }
        }

        //create thumbnail from cover
        public static void CreateThumbnail(Codex c)
        {
            int newwidth = 200; //sets resolution of thumbnail in pixels
            using (MagickImage image = new MagickImage(c.CoverArt))
            {
                //preserve aspect ratio
                int width = image.Width;
                int height = image.Height;
                int newheight = newwidth / width * height;
                //create thumbnail
                image.Thumbnail(newwidth, newheight);
                image.Write(c.Thumbnail);
            }
        }

        //Take screenshot of specific html element 
        public static MagickImage GetCroppedScreenShot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size)
        {
            //take the screenshot
            Screenshot ss = ((ITakesScreenshot)driver).GetScreenshot();
            var img = Image.FromStream(new MemoryStream(ss.AsByteArray)) as Bitmap;

            var imgcropped = img.Clone(new Rectangle(location, size), img.PixelFormat);
            var mf = new MagickFactory();
            MagickImage Magickimg = new MagickImage(mf.Image.Create(imgcropped));
            return Magickimg;
        }
    }
}
