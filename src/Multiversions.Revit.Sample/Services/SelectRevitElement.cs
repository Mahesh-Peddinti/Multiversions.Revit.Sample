using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Services
{
    public class SelectRevitElement : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Select MEP Component");
            Element element = doc.GetElement(reference);

            TaskDialog.Show("Revit", $"Selected MEP Element :{element.Name.ToString()}");
        }

        public string GetName()
        {
            return "SelectRevitElement";
        }
    }
}
