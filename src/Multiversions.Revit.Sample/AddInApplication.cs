using Autodesk.Revit.UI;
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
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("MBDEVTOOLS");
            PushButton pushButton = ribbonPanel.AddItem(new PushButtonData("Button", "ToolTester",
                                                @"D:\REVIT API_TOOLS\MultiversionAddIn\New folder\src\bin\Debug\2026\Multiversions.Revit.Sample.dll",
                                                "Multiversions.Revit.Sample.AddInCommand")) as PushButton;   
           
            pushButton.Image = new BitmapImage(new Uri(@"D:\REVIT API_TOOLS\MultiversionAddIn\New folder\src\Multiversions.Revit.Sample\Resources\ToolIcon.png"));
            pushButton.LargeImage = new BitmapImage(new Uri(@"D:\REVIT API_TOOLS\MultiversionAddIn\New folder\src\Multiversions.Revit.Sample\Resources\ToolIcon.png"));

            
            return Result.Succeeded;
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }         
    }
}
