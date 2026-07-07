using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Models
{

    public class BusDuctCreation : IExternalEventHandler
    {
        private List< EquipmentElement > _equipmentElement;
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
            //Element ductType = new FilteredElementCollector(doc)
            //                                    .OfClass(typeof(DuctType))
            //                                    .First(x => x.Name.Contains(SelectedDuctType));
            //Element ductSystemType = new FilteredElementCollector(doc)
            //                                    .OfClass(typeof(MechanicalSystemType))
            //                                    .First(x => x.Name.Contains("Mitered Elbows / Taps"));
            
            //Level level = new FilteredElementCollector(doc)
            //                                    .OfClass(typeof(Level))
            //                                    .FirstOrDefault(x => x.Name.Contains(SelectedDuctLevel)) as Level;
            
            Func<Element, bool> isRectangularRadiusDuctType = dt => dt.Name.Contains(SelectedDuctType);

            Element ductType = new FilteredElementCollector(doc)
                                .OfClass(typeof(DuctType))
                                .First<Element>(isRectangularRadiusDuctType);

            // Find Level 1 to place ductwork

            Func<Level, bool> isLevel1
              = level => level.Name.Equals("Level 1");

            Level level1 = new FilteredElementCollector(doc)
              .OfClass(typeof(Level))
              .Cast<Level>()
              .First<Level>(isLevel1);

            // Find all mechanical equipment elements

            List<BuiltInCategory> cats = new List<BuiltInCategory>();

            cats.Add(BuiltInCategory.OST_MechanicalEquipment);
            cats.Add(BuiltInCategory.OST_DuctTerminal);

            FilteredElementCollector equipment = new FilteredElementCollector(doc)
                                                .OfClass(typeof(FamilyInstance))
                                                .WherePasses(new ElementMulticategoryFilter(cats));



            //
            List<FamilyInstance> familyInstances = new List<FamilyInstance>();
            //familyInstances.Add(StartConnector.Owner as FamilyInstance);
            //familyInstances.AddRange(EndConnectorSet.Cast<Connector>().Select(x => x.Owner as FamilyInstance));
            List<EquipmentElement> equipmentElements = new List<EquipmentElement>();                       

            foreach (FamilyInstance fi in equipment)
            {
                // find connectors

                ConnectorManager cm = fi.MEPModel.ConnectorManager;

                ConnectorSet cs = cm.Connectors;

                foreach (Connector c in cs)
                {
                    if (Domain.DomainHvac == c.Domain && DuctSystemType.SupplyAir == c.DuctSystemType)
                    {
                        // connector point and direction

                        XYZ p = c.Origin;
                        XYZ v = c.CoordinateSystem.BasisZ;

                        equipmentElements.Add(new EquipmentElement(fi, c, p, v));
                    }
                }
            }

            //way point
            double[] min = new double[] {double.PositiveInfinity,double.PositiveInfinity };

            double[] max = new double[] {double.NegativeInfinity,double.NegativeInfinity };

            foreach (EquipmentElement e in equipmentElements)
            {
                BoundingBoxXYZ box
                  = e.FamilyInstance.get_BoundingBox(null);

                for (int i = 0; i < 2; ++i)
                {
                    min[i] = box.Min[i] < min[i] ? box.Min[i] : min[i];
                    max[i] = box.Max[i] > max[i] ? box.Max[i] : max[i];
                }
            }

            double verticalLocation = level1.Elevation + 11.0;

            XYZ wayPoint = new XYZ(
              (min[0] + max[0]) / 2,
              (min[1] + max[1]) / 2,
              verticalLocation);

            Debug.Print("Waypoint found at {0}", (wayPoint));

            TransactionGroup tGroup = new TransactionGroup(doc);

            tGroup.Start("Auto-route placeholders");

            // Create a new MEP mechanical system 
            // element from all connectors:

            Transaction t = new Transaction(doc);

            t.Start("Create system");

            Connector baseConnector = null;
            ConnectorSet newSystemCS = new ConnectorSet();

            foreach (EquipmentElement e in equipmentElements)
            {
                if (e.FamilyInstance.Category.Id.Equals(
                  new ElementId(BuiltInCategory.OST_MechanicalEquipment)))
                {
                    baseConnector = e.SupplyAirConnector;
                }
                else
                {
                    newSystemCS.Insert(e.SupplyAirConnector);
                }
            }
            MechanicalSystem mechanicalSystem = doc.Create.NewMechanicalSystem(baseConnector,
              newSystemCS, DuctSystemType.SupplyAir);

            t.Commit();

            bool xFirst = true;

            List<Connector> wayPointConnectors = new List<Connector>();

            foreach (EquipmentElement e in equipmentElements)
            {
                Connector nextConnector;

                // if connector direction is vertical, 
                // add duct to reach target elevation

                if (!e.ConnectionDirection.IsAlmostEqualTo(XYZ.BasisZ))
                {
                    throw new NotImplementedException(
                      "Not implemented for initially non-vertical connectors");
                }

                t.Start("Create placeholder duct");

                XYZ secondPoint = new XYZ(e.ConnectionPoint.X,
                  e.ConnectionPoint.Y, wayPoint.Z);

                Duct duct = Duct.CreatePlaceholder(doc, 
                                                    mechanicalSystem.Id,
                                                    ductType.Id,                                                    
                                                    level1.Id, 
                                                    e.ConnectionPoint,
                                                    secondPoint);

                t.Commit();

                t.Start("Connect duct");

                Connector targetConnector = GetDuctConnectorAt(
                  duct, e.ConnectionPoint, out nextConnector);

                targetConnector.ConnectTo(e.SupplyAirConnector);

                t.Commit();

                // all connections should make a right 
                // hand turn into the waypoint

                XYZ nextConnectorPoint = nextConnector.Origin;
                XYZ nextWayPoint = null;
                if (xFirst)
                {
                    nextWayPoint = new XYZ(wayPoint.X,
                      nextConnectorPoint.Y, wayPoint.Z);
                }
                else
                {
                    nextWayPoint = new XYZ(nextConnectorPoint.X,
                      wayPoint.Y, wayPoint.Z);
                }

                t.Start("Create placeholder duct");

                Duct nextDuct = Duct.CreatePlaceholder(doc,
                                                        mechanicalSystem.Id,
                                                        ductType.Id,                                                         
                                                        level1.Id, 
                                                        nextConnectorPoint,
                                                        nextWayPoint);

                t.Commit();

                t.Start("Add fitting");

                Connector nextNextConnector;

                Connector nextTargetConnector
                  = GetDuctConnectorAt(nextDuct,
                    nextConnectorPoint, out nextNextConnector);

                doc.Create.NewElbowFitting(nextConnector,
                  nextTargetConnector);

                t.Commit();

                t.Start("Create placeholder duct");

                nextDuct = Duct.CreatePlaceholder(doc, 
                                                    mechanicalSystem.Id, 
                                                    ductType.Id, 
                                                    level1.Id,
                                                    nextNextConnector.Origin, 
                                                    wayPoint);

                t.Commit();

                t.Start("Add fitting");

                Connector lastConnector;

                Connector nextNextTargetConnector = GetDuctConnectorAt(nextDuct,nextNextConnector.Origin,out lastConnector);

                doc.Create.NewElbowFitting(nextNextConnector,
                  nextNextTargetConnector);

                wayPointConnectors.Add(lastConnector);

                t.Commit();

                xFirst = !xFirst;
            }

            if (wayPointConnectors.Count != 4)
            {
                throw new Exception(
                  "Unexpected number of connectors");
            }

            t.Start("Add cross fitting");

            doc.Create.NewCrossFitting(
              wayPointConnectors[0], wayPointConnectors[2],
              wayPointConnectors[1], wayPointConnectors[3]);

            t.Commit();

            tGroup.Assimilate();
            TaskDialog.Show("Duct", "Completed");
        }
        

        public string GetName()
        {
            return "BusDuctCreation";
        }

        static Connector GetDuctConnectorAt(Duct duct, XYZ location, out Connector otherConnector)
        {
            otherConnector = null;

            Connector targetConnector = null;

            ConnectorManager cm = duct.ConnectorManager;

            foreach (Connector c in cm.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(location))
                {
                    targetConnector = c;
                }
                else
                {
                    otherConnector = c;
                }
            }
            return targetConnector;

        }

    }
}
