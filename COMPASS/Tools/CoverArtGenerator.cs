using COMPASS.Models;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COMPASS
{
    public static class CoverArtGenerator
    { 
        //Convert PDFs to image previews
        public static void ConvertPDF(MyFile pdf, string folder)
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

        //convert image to image preview
        public static void ConvertImage(string imagepath, MyFile destfile, string folder)
        {
            using (MagickImage image = new MagickImage(imagepath))
            {
                if (image.Width > 600) image.Resize(600, 0);
                image.Write(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + folder + @"\CoverArt\" + destfile.ID.ToString() + ".png");
            }
        }
    }
}
