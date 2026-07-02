using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Ribbon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Multiversions.Revit.Sample
{
    public class AddInApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            RibbonBuilder.Build(application);

            return Result.Succeeded;
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }         
    }
}
