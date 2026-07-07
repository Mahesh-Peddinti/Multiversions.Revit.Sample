using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Multiversions.Revit.Sample.Storage;
using System;
using System.Collections.Generic;

namespace Multiversions.Revit.Sample.Services
{
    public class RevitDataService
    {
        private readonly Autodesk.Revit.DB.Document _doc;

        public RevitDataService(Autodesk.Revit.DB.Document document)
        {
            _doc = document;
        }

        public List<SystemTypeDto> GetSystemTypes()
        {
            var result = new List<SystemTypeDto>();

            var collector = new FilteredElementCollector(_doc).OfClass(typeof(MEPSystemType));
            foreach (Element e in collector)
            {
                var sys = e as MEPSystemType;
                if (sys == null) continue;

                result.Add(new SystemTypeDto
                {
                    ID = sys.Id,
                    Name = sys.Name
                });
            }

            return result;
        }

        public List<DuctTypeDto> GetDuctTypes()
        {
            var result = new List<DuctTypeDto>();

            var collector = new FilteredElementCollector(_doc).OfClass(typeof(DuctType));
            foreach (Element e in collector)
            {
                var dt = e as DuctType;
                if (dt == null) continue;

                result.Add(new DuctTypeDto
                {
                    Id = dt.Id,
                    Name = dt.Name
                });
            }

            return result;
        }

        public List<LevelDto> GetLevels()
        {
            var result = new List<LevelDto>();

            var collector = new FilteredElementCollector(_doc).OfClass(typeof(Level));
            foreach (Element e in collector)
            {
                var lvl = e as Level;
                if (lvl == null) continue;

                result.Add(new LevelDto
                {
                    Id = lvl.Id,
                    Name = lvl.Name
                });
            }

            return result;
        }
    }
}
