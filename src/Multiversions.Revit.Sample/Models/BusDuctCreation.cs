using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Multiversions.Revit.Sample.Models
{
    public class BusDuctCreation : IExternalEventHandler
    {
        // Properties bound from ViewModel
        public Connector StartConnector { get; set; }
        public ConnectorSet EndConnectorSet { get; set; }
        public string SelectedDuctType { get; set; }
        public string SelectedDuctLevel { get; set; }
        public string SelectedDuctSystemType { get; set; }

        public void Execute(UIApplication app)
        {
            Document doc = app.ActiveUIDocument.Document;

            // Resolve duct type
            DuctType ductType = new FilteredElementCollector(doc)
                                .OfClass(typeof(DuctType))
                                .Cast<DuctType>()
                                .FirstOrDefault(x => x.Name.Equals(SelectedDuctType));

            if (ductType == null)
                throw new InvalidOperationException($"DuctType '{SelectedDuctType}' not found.");

            // Resolve system type
            MechanicalSystemType ductSystemType = new FilteredElementCollector(doc)
                                .OfClass(typeof(MechanicalSystemType))
                                .Cast<MechanicalSystemType>()
                                .FirstOrDefault(x => x.Name.Contains(SelectedDuctSystemType));

            if (ductSystemType == null)
                throw new InvalidOperationException($"SystemType '{SelectedDuctSystemType}' not found.");

            // Resolve level
            Level level = new FilteredElementCollector(doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .FirstOrDefault(x => x.Name.Contains(SelectedDuctLevel));

            if (level == null)
                throw new InvalidOperationException($"Level '{SelectedDuctLevel}' not found.");

            // Collect equipment
            List<BuiltInCategory> cats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_DuctTerminal
            };

            var equipment = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .WherePasses(new ElementMulticategoryFilter(cats))
                .Cast<FamilyInstance>();

            List<EquipmentElement> equipmentElements = new List<EquipmentElement>();

            foreach (FamilyInstance fi in equipment)
            {
                ConnectorManager cm = fi.MEPModel?.ConnectorManager;
                if (cm == null) continue;

                foreach (Connector c in cm.Connectors)
                {
                    if (c.Domain == Domain.DomainHvac && c.DuctSystemType == DuctSystemType.SupplyAir)
                    {
                        XYZ p = c.Origin;
                        XYZ v = c.CoordinateSystem.BasisZ;
                        equipmentElements.Add(new EquipmentElement(fi, c, p, v));
                    }
                }
            }

            if (!equipmentElements.Any())
                throw new InvalidOperationException("No valid equipment connectors found.");

            // Compute waypoint
            double[] min = { double.PositiveInfinity, double.PositiveInfinity };
            double[] max = { double.NegativeInfinity, double.NegativeInfinity };

            foreach (EquipmentElement e in equipmentElements)
            {
                BoundingBoxXYZ box = e.FamilyInstance.get_BoundingBox(null);
                if (box == null) continue;

                for (int i = 0; i < 2; ++i)
                {
                    min[i] = Math.Min(min[i], box.Min[i]);
                    max[i] = Math.Max(max[i], box.Max[i]);
                }
            }

            double verticalLocation = level.Elevation + 11.0;
            XYZ wayPoint = new XYZ((min[0] + max[0]) / 2, (min[1] + max[1]) / 2, verticalLocation);

            Debug.Print("Waypoint found at {0}", wayPoint);

            using (TransactionGroup tGroup = new TransactionGroup(doc, "Auto-route placeholders"))
            {
                tGroup.Start();

                using (Transaction t = new Transaction(doc, "Create system"))
                {
                    t.Start();

                    Connector baseConnector = null;
                    ConnectorSet newSystemCS = new ConnectorSet();

                    foreach (EquipmentElement e in equipmentElements)
                    {
                        if (e.FamilyInstance.Category.Id.Equals(new ElementId(BuiltInCategory.OST_MechanicalEquipment)))
                            baseConnector = e.SupplyAirConnector;
                        else
                            newSystemCS.Insert(e.SupplyAirConnector);
                    }

                    if (baseConnector == null)
                        throw new InvalidOperationException("No base connector found.");

                    doc.Create.NewMechanicalSystem(baseConnector, newSystemCS, DuctSystemType.SupplyAir);

                    t.Commit();
                }

                // Routing ducts to waypoint
                bool xFirst = true;
                List<Connector> wayPointConnectors = new List<Connector>();

                foreach (EquipmentElement e in equipmentElements)
                {
                    Connector nextConnector;

                    if (!e.ConnectionDirection.IsAlmostEqualTo(XYZ.BasisZ))
                        throw new NotImplementedException("Non-vertical connectors not implemented.");

                    using (Transaction t = new Transaction(doc, "Create placeholder duct"))
                    {
                        t.Start();

                        XYZ secondPoint = new XYZ(e.ConnectionPoint.X, e.ConnectionPoint.Y, wayPoint.Z);

                        Duct duct = Duct.CreatePlaceholder(doc,
                            ductSystemType.Id, ductType.Id, level.Id,
                            e.ConnectionPoint, secondPoint);

                        Debug.Print("Created placeholder duct from {0} to {1}", e.ConnectionPoint, secondPoint);

                        Connector targetConnector = GetDuctConnectorAt(duct, e.ConnectionPoint, out nextConnector);
                        targetConnector.ConnectTo(e.SupplyAirConnector);
                        Debug.Print("Created placeholder duct from {0} to {1},{2}", targetConnector.Origin, nextConnector.Origin, (e.SupplyAirConnector).Origin);
                        t.Commit();
                    }

                    // Right-angle turn into waypoint
                    XYZ nextConnectorPoint = nextConnector.Origin;
                    XYZ nextWayPoint = xFirst
                        ? new XYZ(wayPoint.X, nextConnectorPoint.Y, wayPoint.Z)
                        : new XYZ(nextConnectorPoint.X, wayPoint.Y, wayPoint.Z);

                    xFirst = !xFirst;

                    using (Transaction t = new Transaction(doc, "Route to waypoint"))
                    {
                        t.Start();

                        Duct nextDuct = Duct.CreatePlaceholder(doc,
                            ductSystemType.Id, ductType.Id, level.Id,
                            nextConnectorPoint, nextWayPoint);

                        Connector nextNextConnector;
                        Connector nextTargetConnector = GetDuctConnectorAt(nextDuct, nextConnectorPoint, out nextNextConnector);

                        doc.Create.NewElbowFitting(nextConnector, nextTargetConnector);

                        Duct finalDuct = Duct.CreatePlaceholder(doc,
                            ductSystemType.Id, ductType.Id, level.Id,
                            nextNextConnector.Origin, wayPoint);

                        Connector lastConnector;
                        Connector nextNextTargetConnector = GetDuctConnectorAt(finalDuct, nextNextConnector.Origin, out lastConnector);

                        doc.Create.NewElbowFitting(nextNextConnector, nextNextTargetConnector);

                        wayPointConnectors.Add(lastConnector);

                        t.Commit();
                    }
                }

                if (wayPointConnectors.Count != 5)
                    throw new Exception("Unexpected number of connectors at waypoint.");

                using (Transaction t = new Transaction(doc, "Add cross fitting"))
                {
                    t.Start();
                    doc.Create.NewCrossFitting(
                        wayPointConnectors[0], wayPointConnectors[2],
                        wayPointConnectors[1], wayPointConnectors[3]);
                    t.Commit();
                }

                tGroup.Assimilate();
            }

            TaskDialog.Show("Duct", "Bus duct creation completed.");
        }

        public string GetName() => "BusDuctCreation";

        private static Connector GetDuctConnectorAt(Duct duct, XYZ location, out Connector otherConnector)
        {
            otherConnector = null;
            Connector targetConnector = null;

            ConnectorManager cm = duct.ConnectorManager;
            foreach (Connector c in cm.Connectors)
            {
                if (c.Origin.IsAlmostEqualTo(location))
                    targetConnector = c;
                else
                    otherConnector = c;
            }
            return targetConnector ?? throw new InvalidOperationException("Target connector not found.");
        }
    }
}
