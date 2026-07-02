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

    public class BusDuctCreation : IExternalEventHandler
    {
        //This class is responsible for creating bus duct in Revit using the selected start and end connectors
        //implement all the properties to recieve value from the view model and use them in the Execute method to create bus duct
        public Connector StartConnector { get; set; }
        public Connector EndConnector { get; set; }
        public string SelectedDuctType { get; set; }
        public string SelectedDuctLevel { get; set; }
        public string SelectedDuctSystemType { get; set; }

        public void Execute(UIApplication app)
        {          

            Document doc = app.ActiveUIDocument.Document;
            //String formatting for the selected values
            string info = $"Selected Duct Type: {SelectedDuctType}, Selected Duct Level: {SelectedDuctLevel}, Selected Duct System Type: {SelectedDuctSystemType}" +
                $"Connector 1: {(StartConnector.MEPSystem)?.ToString()}";

            TaskDialog.Show("Revit", info);
            /*
            try
            {

                //Using Transaction group to create bus duct
                using(TransactionGroup tg = new TransactionGroup(doc))
                {
                    tg.Start();
                    //Using Ducting system
                    using(Transaction tx = new Transaction(doc))
                    {
                        tx.Start();
                        //doc.Create.NewMechanicalSystem();
                        tx.Commit();
                    }
                }
                using(Transaction txPlaceHolder = new Transaction(doc,"Creating Duct Place Holders"))
                {
                    txPlaceHolder.Start();
                    //Duct.CreatePlaceholder(doc,);
                    txPlaceHolder.Commit();


                }

                using(Transaction txConvert = new Transaction(doc,"Convert Duct Place Holder in to duct"))
                {
                    txConvert.Start();
                    //MechanicalUtils.ConvertDuctPlaceholders(doc,);
                    txConvert.Commit();

                }

            }
            catch (Exception e)
            {

                throw ;
            }
            */

        }


        public string GetName()
        {
            return "BusDuctCreation";
        }
    }
}
