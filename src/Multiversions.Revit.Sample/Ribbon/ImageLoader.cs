using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Multiversions.Revit.Sample.Ribbon
{
    public static class ImageLoader
    {
        // Cache decoded, frozen BitmapImage instances keyed by resource name.
        private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new ConcurrentDictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

        public static BitmapImage Load(string imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return null;

            return _cache.GetOrAdd(imageName, key =>
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"Multiversions.Revit.Sample.Resources.{key}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;

                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad; // ensure data is loaded while stream is open
                    image.EndInit();
                    image.Freeze(); // make it shareable across threads and reduce overhead
                    return image;
                }
            });
        }
    }
}
