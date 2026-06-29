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
        
        public string _SystemClassificationName { get; set; }
        public void Execute(UIApplication app)
        {
           

            Document doc = app.ActiveUIDocument.Document;
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
                        
                        tx.Commit();
                    }
                }

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
