using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Storage
{
    public class EquipmentDto
    {
        public string Name { get; set; }
        public XYZ Location { get; set; }
        public ElementId Id { get; set; }
    }
}
