using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Models
{
    public class TheThemeChanger : IExternalEventHandler
    {
        void IExternalEventHandler.Execute(UIApplication app)
        {

            UITheme uiTheme = UIThemeManager.CurrentTheme;

            switch(uiTheme)
            {
                case UITheme.Dark:
                    UIThemeManager.CurrentTheme = UITheme.Light;
                    break;
                case UITheme.Light:
                    UIThemeManager.CurrentTheme = UITheme.Dark;
                    break;
            }
            
        }

        string IExternalEventHandler.GetName()
        {
            return "TheThemeChanger";
        }
    }
}
