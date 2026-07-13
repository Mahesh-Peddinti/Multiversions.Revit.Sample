using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.Linq;


namespace Multiversions.Revit.Sample.Models
{
    public class CollectAndRenameElements : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            Autodesk.Revit.DB.Document doc = app.ActiveUIDocument.Document;
            Collection<Reference> walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>() as Collection<Reference>;

        }

        public string GetName()
        {
            return "Collect and Rename Elements";
        }
    }
}
