using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Multiversions.Revit.Sample.Services;
using Multiversions.Revit.Sample.Storage;
using Multiversions.Revit.Sample.ViewModels;
using Multiversions.Revit.Sample.Views;
using System.Collections.ObjectModel;


namespace Multiversions.Revit.Sample
{
    /// <summary>
    /// A sample command.
    /// </summary>
    /// <seealso cref="T:Autodesk.Revit.UI.IExternalCommand" />
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AddInCommand : IExternalCommand
    {
        /// <summary>
        /// Executes the specified Revit command <see cref="ExternalCommand"/>.
        /// The main Execute method (inherited from IExternalCommand) must be public.
        /// </summary>
        /// <param name="commandData">The command data / context.</param>
        /// <param name="message">The message.</param>
        /// <param name="elements">The elements.</param>
        /// <returns>The result of command execution.</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            var selection = uidoc.Selection;
            //Revit Data
            RevitDataService service = new RevitDataService(doc);

            var systems = service.GetSystemTypes();

            var ducts = service.GetDuctTypes();

            var levels = service.GetLevels();


            //UI implimentation
            var vm = new MainViewModel(systems,ducts,levels);
            
            //Show the main window of the application
            var mainWindow = new TestWindow { DataContext = vm };
            mainWindow.Show();
            return Result.Succeeded;
        }
    }  

}