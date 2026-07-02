using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Multiversions.Revit.Sample.Ribbon
{
    public static class ImageLoader
    {
        public static BitmapImage Load(string imageName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            Stream stream = assembly.GetManifestResourceStream($"Multiversions.Revit.Sample.Resources.{imageName}");

            BitmapImage image = new BitmapImage();

            image.BeginInit();
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            return image;
        }
    }
}
