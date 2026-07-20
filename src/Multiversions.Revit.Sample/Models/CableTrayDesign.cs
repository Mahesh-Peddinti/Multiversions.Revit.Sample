using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Models
{
    public class CableTrayDesign : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            //Design Cable by using 2 selected connectors
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            Reference startRef = app.ActiveUIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select Start Connector");
            Reference endRef = app.ActiveUIDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select End Connector");
            //Get the selected connectors by using Family manager and ConnectorManager
            Element startElement = app.ActiveUIDocument.Document.GetElement(startRef);
            Element endElement = app.ActiveUIDocument.Document.GetElement(endRef);
            //consider the selectede elements are family instances and have connectors
            FamilyInstance startFamilyInstance = startElement as FamilyInstance;
            FamilyInstance endFamilyInstance = endElement as FamilyInstance;
            //get connectors from the family instances
            ConnectorSet startConnectors = startFamilyInstance.MEPModel.ConnectorManager.Connectors;
            ConnectorSet endConnectors = endFamilyInstance.MEPModel.ConnectorManager.Connectors;
            //get the first connector from the connector set
            Connector startConnector = startConnectors.Cast<Connector>().FirstOrDefault();
            Connector endConnector = endConnectors.Cast<Connector>().FirstOrDefault();

            // Get required parameters for CableTray.Create
            Document doc = app.ActiveUIDocument.Document;
            // Get default CableTray type
            ElementId cableTrayTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.CableTrayType);
            // Get LevelId from start element (or use a default Level)
            ElementId levelId = startElement.LevelId;
            // Use connector origins as start and end points
            XYZ startPoint = startConnector?.Origin;
            XYZ endPoint = endConnector?.Origin;

            using (Transaction trans = new Transaction(doc, "Create Cable Tray"))
            {
                trans.Start();
                CableTray cableTray = CableTray.Create(doc, cableTrayTypeId, startPoint, endPoint, levelId);
                trans.Commit();
            }
        }

        public string GetName()
        {
            return "CableTrayDesign";
        }
    }
}
