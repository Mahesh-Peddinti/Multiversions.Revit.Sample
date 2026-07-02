using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Multiversions.Revit.Sample.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Services
{
    public class SelectRevitElement : IExternalEventHandler
    {
        public  DuctDataStorage _ductDataStorage;
        public void Execute(UIApplication app)
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Select MEP Component");
            FamilyInstance element = doc.GetElement(reference) as FamilyInstance;
            if (element != null || element is FamilyInstance)
            {                
                var connectors = element.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>();
                foreach (Connector c in connectors)
                {
                    if (c != null && c.DuctSystemType == DuctSystemType.SupplyAir)
                    {
                        //store the connector in SelectRevitElement class for later use
                        _ductDataStorage = new DuctDataStorage();
                        _ductDataStorage.StartConnector = c;
                    }
                }

            }

            TaskDialog.Show("Revit", $"Selected MEP Element :{element.Name.ToString()}");
        }

        public string GetName()
        {
            return "SelectRevitElement";
        }
    }
}
