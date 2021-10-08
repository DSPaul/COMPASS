using COMPASS.Models;
using COMPASS.Tools;
using ImageMagick;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace COMPASS
{
    public static class CoverArtGenerator
    { 
        //Convert PDFs to image previews
        public static void ConvertPDF(Codex pdf, string folder)
        {
            MagickReadSettings settings = new MagickReadSettings()
            {
                Density = new Density(100, 100),
                FrameIndex = 0, // First page
                FrameCount = 1, // Number of pages
            };
            using (MagickImage image = new MagickImage())
            {
                image.Read(pdf.Path, settings);
                image.Format = MagickFormat.Png;
                image.BackgroundColor = new MagickColor("#000000"); //set backgroun color as transparant
                image.Border(20); //adds transparant border around image
                image.Trim(); //cut off all transparancy
                image.RePage(); //resize image to fit what was cropped

                image.Write(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + folder +  @"\CoverArt\" + pdf.ID.ToString() + ".png");
            }
        }

        //Get coverart fom URL
        public static void GetCoverFromURL(string URL, Codex destfile, Enums.ImportMode import)
        {
            //Use Selenium for screenshotting pages
            var driverService = ChromeDriverService.CreateDefaultService();
            driverService.HideCommandPromptWindow = true;
            ChromeOptions CO = new ChromeOptions();
            CO.AddArgument("--window-size=2500,2000");
            CO.AddArgument("--headless");
            ChromeDriver driver = new ChromeDriver(driverService, CO);

            IWebElement Coverpage = null;
            MagickImage image = null;

            try
            {
                driver.Navigate().GoToUrl(URL);

                switch (import)
                {
                    case Enums.ImportMode.GmBinder:
                        Coverpage = driver.FindElementById("p1");
                        //screenshot and download the image
                        image = GetCroppedScreenShot(driver, Coverpage.Location, Coverpage.Size);
                        break;

                    case Enums.ImportMode.Homebrewery:
                        //get nav height because scraper doesn't see nav anymore after it switched to frame
                        var nav = driver.FindElementByXPath("//nav");
                        string navhstr = nav.GetCssValue("height");
                        float navheight = float.Parse(navhstr.Substring(0, navhstr.Length - 2), CultureInfo.InvariantCulture);

                        //switch to iframe
                        var iframe = driver.FindElementByXPath("//iframe");
                        driver.SwitchTo().Frame(iframe);
                        Coverpage = driver.FindElementById("p1");
                        //shift page down by nav height because element location is relative to frame, but coords on screenshot is reletaive to page 
                        int newY = (int)Math.Round(Coverpage.Location.Y + navheight, 0);

                        //screenshot and download the image
                        image = GetCroppedScreenShot(driver, new Point(Coverpage.Location.X, newY), Coverpage.Size);
                        break;
                }
            }
            catch { }

            driver.Quit();

            if (image.Width > 850) image.Resize(850, 0);
            image.Write(destfile.CoverArt);
        }

        //convert image to image preview
        public static void SaveImageAsCover(string imagepath, Codex destfile)
        {
            using (MagickImage image = new MagickImage(imagepath))
            {
                if (image.Width > 600) image.Resize(600, 0);
                image.Write(destfile.CoverArt);
            }
        }

        //Take screenshot of specific html element 
        public static MagickImage GetCroppedScreenShot(IWebDriver driver, Point location, Size size)
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
