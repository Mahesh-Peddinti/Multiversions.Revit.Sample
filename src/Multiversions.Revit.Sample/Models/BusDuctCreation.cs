using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Models
{ 

    public class BusDuctCreation : IExternalEventHandler
    {
        //This class is responsible for creating bus duct in Revit using the selected start and end connectors
        //implement all the properties to recieve value from the view model and use them in the Execute method to create bus duct
        public Connector StartConnector { get; set; }
        public ConnectorSet EndConnectorSet { get; set; }
        public string SelectedDuctType { get; set; }
        public string SelectedDuctLevel { get; set; }
        public string SelectedDuctSystemType { get; set; }
        

        public void Execute(UIApplication app)
        {          

            Document doc = app.ActiveUIDocument.Document;
            /*
            //String formatting for the selected values
            string info = $"Selected Duct Type: {SelectedDuctType}\n"+
                $"Selected Duct Level: {SelectedDuctLevel}\n"+
                $"Selected Duct System Type: {SelectedDuctSystemType}\n" +
                $"Connector 1: {(StartConnector.Shape.ToString())}\n" +
                $"ConnectorSet : {EndConnectorSet.Size}";

            TaskDialog.Show("Revit", info);
            */
            Element ductType = new FilteredElementCollector(doc)
                                                .OfClass(typeof(DuctType))                                                
                                                .First(x => x.Name.Contains(SelectedDuctType));
            Element ductSystemType = new FilteredElementCollector(doc)
                                                .OfClass(typeof(MechanicalSystemType))
                                                .First(x => x.Name.Contains(SelectedDuctSystemType));
            Element level = new FilteredElementCollector(doc)
                                                .OfClass(typeof(Level))
                                                .First(x => x.Name.Contains(SelectedDuctLevel));



            try
            {
                //Using Transaction group to create bus duct
                
                
                using (TransactionGroup tg = new TransactionGroup(doc))
                {
                    tg.Start();
                    using (Transaction tx = new Transaction(doc, "Creating Bus Duct System"))
                    {
                        tx.Start();
                        doc.Create.NewMechanicalSystem(StartConnector, EndConnectorSet, DuctSystemType.SupplyAir);
                        tx.Commit();
                    }
                    //Using Ducting system
                    foreach (Connector c in EndConnectorSet)
                    {
                        using (Transaction txPlaceHolder = new Transaction(doc, "Creating Duct Place Holders"))
                        {
                            txPlaceHolder.Start();
                            XYZ point = StartConnector.Origin as XYZ;
                            XYZ endPoint = c.Origin as XYZ;
                            Duct.CreatePlaceholder(doc, ductSystemType.Id, ductType.Id,level.Id, point, endPoint);
                            txPlaceHolder.Commit();
                        }
                    }                    
                }
                
               
                /*
                using(Transaction txConvert = new Transaction(doc,"Convert Duct Place Holder in to duct"))
                {
                    txConvert.Start();
                    //MechanicalUtils.ConvertDuctPlaceholders(doc,);
                    txConvert.Commit();
                }
                */

            }
            catch (Exception e)
            {

                throw ;
            }  
            

        }

        public string GetName()
        {
            return "BusDuctCreation";
        }
    }
}
