namespace COMPASS.Infra.Tools;

public static class FileFormatUtils
{
    public static bool IsImageFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string extension = Path.GetExtension(path).ToLower();
        List<string> imgExtensions =
        [
            ".png",
            ".jpg",
            ".jpeg",
            ".webp"
        ];

        return imgExtensions.Contains(extension);
    }

    public static bool IsPDFFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return Path.GetExtension(path).ToLower() == ".pdf";
    }
}