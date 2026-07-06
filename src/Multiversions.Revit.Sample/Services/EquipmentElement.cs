using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Services
{
    public class EquipmentElement
    {
        public FamilyInstance FamilyInstance;
        public Connector SupplyAirConnector;
        public XYZ ConnectionPoint;
        public XYZ ConnectionDirection;

        public EquipmentElement(
          FamilyInstance familyInstance,
          Connector supplyAirConnector,
          XYZ connectionPoint,
          XYZ connectionDirection)
        {
            FamilyInstance = familyInstance;
            SupplyAirConnector = supplyAirConnector;
            ConnectionPoint = connectionPoint;
            ConnectionDirection = connectionDirection;
        }
    }
}
