using Autodesk.Revit.UI;
using System.Reflection;

namespace Multiversions.Revit.Sample.Ribbon
{
    public static class RibbonButtonFactory
    {
        // Cache assembly path to avoid repeated reflection calls
        private static readonly string _assemblyPath = Assembly.GetExecutingAssembly().Location;

        public static PushButtonData Create(
            RibbonButtonInfo info)
        {
            PushButtonData button =
                new PushButtonData(
                    info.Name,
                    info.Text,
                    _assemblyPath,
                    info.CommandClass);

            button.ToolTip = info.Tooltip;

            // Load images via ImageLoader (which now caches and disposes streams)
            button.LargeImage = ImageLoader.Load(info.LargeImage);
            button.Image = ImageLoader.Load(info.SmallImage);

            return button;
        }
    }
}
