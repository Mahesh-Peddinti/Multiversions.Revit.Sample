using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Storage
{
    public class LevelDto
    {
        public ElementId Id { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }
}
