using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Models
{
    public class ConvertPlaceholderToDuct : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            //get the document from the UIApplication

            Document doc = app.ActiveUIDocument.Document;

            Transaction t = new Transaction(doc);
            t.Start("Convert placeholder network");

            FilteredElementCollector ductCollector = new FilteredElementCollector(doc)
                                                    .OfClass(typeof(Duct));

            Func<Duct, bool> isPlaceholder = duct => duct.IsPlaceholder;

            IEnumerable<Duct> ducts = ductCollector
                                      .OfType<Duct>()
                                      .Where<Duct>(isPlaceholder);

            ICollection<ElementId> ductIds = ducts
                                              .Select<Duct, ElementId>(duct => duct.Id)
                                              .ToList<ElementId>();

            MechanicalUtils.ConvertDuctPlaceholders(doc, ductIds);

            t.Commit();          
        }

        public string GetName()
        {
            return "Convert Placeholder Ducts to Real Ducts";
        }
    }
}
