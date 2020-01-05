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
        public static void ConvertPDF(MyFile pdf)
        {
            MagickReadSettings settings = new MagickReadSettings()
            {
                Density = new Density(100, 100),
                FrameIndex = 0, // First page
                FrameCount = 1, // Number of pages
            };
            using (MagickImage image = new MagickImage())
            {
                // Add all the pages of the pdf file to the collection
                image.Read(pdf.Path, settings);
                image.Format = MagickFormat.Png;
                image.Trim();
                image.Alpha(AlphaOption.Remove);

                image.Write(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\CoverArt\" + pdf.ID.ToString() + ".png");
            }
        }
    }
}
