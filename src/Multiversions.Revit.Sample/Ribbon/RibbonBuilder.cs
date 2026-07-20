using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Ribbon
{
    public static class RibbonBuilder
    {
        public static void Build(UIControlledApplication app)
        {
            const string tab = "MBDev-Tools";

            try
            {
                app.CreateRibbonTab(tab);
            }
            catch
            {

            }

            RibbonPanel panel =
                app.CreateRibbonPanel(tab, "MBDevTools");

            panel.AddItem(
                RibbonButtonFactory.Create(
                    new RibbonButtonInfo
                    {
                        Name = "BusDuct",

                        Text = "BusDuct",

                        CommandClass =
                            "Multiversions.Revit.Sample.AddInCommand",

                        Tooltip =
                            "Creates Bus Duct",

                        LargeImage = "ToolIcon.png",

                        SmallImage = "ToolIcon.png"
                    }));

            panel.AddItem(
                RibbonButtonFactory.Create(
                    new RibbonButtonInfo
                    {
                        Name = "Theme",

                        Text = "Theme",

                        CommandClass =
                            "Multiversions.Revit.Sample.AddInCommand",

                        Tooltip =
                            "Creates Bus Duct",

                        LargeImage = "ToolIcon.png",

                        SmallImage = "ToolIcon.png"
                    }));
            panel.AddItem(
                RibbonButtonFactory.Create(
                    new RibbonButtonInfo
                    {
                        Name = "CableTray",

                        Text = "Cable Tray",

                        CommandClass =
                            "Multiversions.Revit.Sample.AddInCommand",

                        Tooltip =
                            "Creates Cable Tray ",

                        LargeImage = "ToolIcon.png",

                        SmallImage = "ToolIcon.png"
                    }));
        }
    }
}
