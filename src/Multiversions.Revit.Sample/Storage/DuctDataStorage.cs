using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multiversions.Revit.Sample.Storage
{
    public class DuctDataStorage : INotifyPropertyChanged
    {

        private Connector _startConnector;
        public Connector StartConnector
        {
            get { return _startConnector; }
            set 
            { 
                _startConnector = value;
                OnPropertyChanged(nameof(StartConnector));
            }
            
        }

        public Connector _endConnector;
        public Connector EndConnector
        {
            get { return _endConnector; }
            set
            {
                _endConnector = value;
                OnPropertyChanged(nameof(EndConnector));
            }
        }

        public ObservableCollection<string> DuctLevels { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Execute(UIDocument uiDoc)
        {
            
            //Levels Loading
            DuctLevels = new ObservableCollection<string>();
            var levels = new FilteredElementCollector(uiDoc.Document)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .ToList();
            foreach (Level level in levels)
            {
                DuctLevels.Add(level.Name);
            }

        }

        public string GetName()
        {
            return "DuctDataStorage";
        }
    }
}
