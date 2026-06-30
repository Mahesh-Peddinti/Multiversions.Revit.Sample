using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Multiversions.Revit.Sample.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return new FilteredElementCollector(_doc)
               .OfClass(typeof(MEPSystemType))
               .Cast<MEPSystemType>()
               .Select(x => new SystemTypeDto
               {
                   ID = x.Id,
                   Name = x.Name
               })
            .ToList();               
        }

        public List<DuctTypeDto> GetDuctTypes()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(DuctType))
                .Cast<DuctType>()
                .Select(x => new DuctTypeDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }

        public List<LevelDto> GetLevels()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Select(x => new LevelDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToList();
        }
    }
}
