using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Models;
using Multiversions.Revit.Sample.Services;
using Multiversions.Revit.Sample.Storage;
using Multiversions.Revit.Sample.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Multiversions.Revit.Sample.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //Constants
        private readonly UIDocument _uiDoc;
        private readonly DuctDataStorage  _ductDataStorage;

        //Implement ICommand
        public ICommand ThemeChangeCommand { get; set; }
        public ICommand SelectStartEquipmentCommand { get; set; }
        public ICommand SelectEndEquipmentCommand { get; set; }
        public ICommand DuctPlaceHolderCreateCommand {  get; set; }

        public ObservableCollection<LevelDto> DuctLevels { get;  set; }
        public ObservableCollection<DuctTypeDto> DuctTypes { get;  set; }
        public ObservableCollection<SystemTypeDto> DuctSystemTypes { get; set; }

        //Properties for the selected values
        private DuctTypeDto _selectedDuctType;
        public DuctTypeDto SelectedDuctType
        {
            get => _selectedDuctType;
            set
            {
                _selectedDuctType = value;
                OnPropertyChanged(nameof(SelectedDuctType));
            }
        }

        private SystemTypeDto _selectedDuctSystemType;
        public SystemTypeDto SelectedDuctSystem
        {
            get => _selectedDuctSystemType;
            set
            {
                _selectedDuctSystemType = value;
                OnPropertyChanged(nameof(SelectedDuctSystem));
            }
        }

        private LevelDto _selectedDuctLevel;
        public LevelDto SelectedLevel
        {
            get => _selectedDuctLevel;
            set
            {
                _selectedDuctLevel = value;
                OnPropertyChanged(nameof(SelectedLevel));
            }
        }
        //Implement the property of Start and End Connector from Selected Revit Element
        private Connector _startConnector;
        public Connector StartConnector
        {
            get => _startConnector;
            set
            {
                _startConnector = value;
                OnPropertyChanged(nameof(StartConnector));
            }
        }
        private Connector _endConnector;
        public Connector EndConnector
        {
            get => _endConnector;
            set
            {
                _endConnector = value;
                OnPropertyChanged(nameof(EndConnector));
            }
        }

        //Theme change set-Up
        private readonly TheThemeChanger _themeChangeEventHandler;
        private readonly ExternalEvent _externalEvent;
        //Element Selection Set-up
        private readonly SelectRevitElement _ElementSelectionEventHandler;
        private readonly ExternalEvent _selectionExternalEvent;
        //Bus Duct Creation 
        private readonly BusDuctCreation _busDuctCreationHandler;
        private readonly ExternalEvent _busDuctCreationEventraiser;

        public MainViewModel(   List<SystemTypeDto> systems,
                                List<DuctTypeDto> ducts,
                                List<LevelDto> levels)
        {
            //Loadthe connector data storage
            _ductDataStorage = new DuctDataStorage();
            //Preloaded
            DuctSystemTypes = new ObservableCollection<SystemTypeDto>(systems);
            DuctTypes = new ObservableCollection<DuctTypeDto>(ducts);
            DuctLevels = new ObservableCollection<LevelDto>(levels);
            //Theme change
            _themeChangeEventHandler = new TheThemeChanger();
            _externalEvent = ExternalEvent.Create(_themeChangeEventHandler);
            ThemeChangeCommand = new RelayCommand( RaiseThemeChangeEvent);

            //Selection
            _ElementSelectionEventHandler = new SelectRevitElement();
            _selectionExternalEvent = ExternalEvent.Create(_ElementSelectionEventHandler);
            SelectStartEquipmentCommand = new RelayCommand(RaiseStartElementSelectionEvent);
            SelectEndEquipmentCommand = new RelayCommand(RaiseEndElementSelectionEvent);
            //Loading selected connector in to the class
            _ElementSelectionEventHandler.Storage = _ductDataStorage;

            //Bus Duct Creation Command 
            _busDuctCreationHandler = new BusDuctCreation();
            _busDuctCreationEventraiser = ExternalEvent.Create(_busDuctCreationHandler);
            DuctPlaceHolderCreateCommand = new RelayCommand(RaiseBusDuctCreationEvent);             

        }
        
        private void RaiseBusDuctCreationEvent()
        {
            //Load all the Data that need to raise the event
            //_busDuctCreationHandler.SelectedDuctType = SelectedDuctType.Name;
            //_busDuctCreationHandler.SelectedDuctSystemType = SelectedDuctSystem.Name;
            //_busDuctCreationHandler.SelectedDuctLevel = SelectedLevel.Name;
            //_busDuctCreationHandler.StartConnector = _ductDataStorage.StartConnector;
            //_busDuctCreationHandler.EndConnectorSet = _ductDataStorage.ConnectorSet;
            
            _busDuctCreationEventraiser.Raise();
        }

        private void RaiseStartElementSelectionEvent()
        {

            _ElementSelectionEventHandler.Request = new ConnectorSelectionRequest
            {
                Message = "SelectionOperation Equipment Connector 1",
                SelectionOperation = SelectionOperation.StartConnector,
            };
            _selectionExternalEvent.Raise();
        }
        private void RaiseEndElementSelectionEvent()
        {
            _ElementSelectionEventHandler.Request = new ConnectorSelectionRequest
            {
                Message = "SelectionOperation Equipment Connector 1",
                SelectionOperation = SelectionOperation.ConnectorSet,
            };
            _selectionExternalEvent.Raise();
        }

        private void RaiseThemeChangeEvent()
        {
            // Change this line in the constructor:
            //ThemeChangeCommand = new RelayCommand(RaiseThemeChangeEvent);
           _externalEvent.Raise();
        }

        //PreLoading properties

        private void OnPropertyChanged(string v)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
