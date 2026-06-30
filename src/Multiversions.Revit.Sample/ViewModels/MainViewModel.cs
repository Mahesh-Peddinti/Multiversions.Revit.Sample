using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Models;
using Multiversions.Revit.Sample.Services;
using Multiversions.Revit.Sample.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Multiversions.Revit.Sample.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //Constants
        private readonly UIDocument _uiDoc;
        

        //Implement ICommand
        public ICommand ThemeChangeCommand { get; set; }
        public ICommand SelectStartEquipmentCommand { get; set; }
        public ICommand DuctPlaceHolderCreateCommand {  get; set; }
        public ObservableCollection<LevelDto> DuctLevels { get;  set; }
        public ObservableCollection<DuctTypeDto> DuctTypes { get;  set; }
        public ObservableCollection<SystemTypeDto> DuctSystemTypes { get; set; }



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
            SelectStartEquipmentCommand = new RelayCommand(RaiseElementSelectionEvent);

            //Bus Duct Creation Command 
            _busDuctCreationHandler = new BusDuctCreation();
            _busDuctCreationEventraiser = ExternalEvent.Create(_busDuctCreationHandler);
            DuctPlaceHolderCreateCommand = new RelayCommand(RaiseBusDuctCreationEvent);
            

        }
        
        private void RaiseBusDuctCreationEvent()
        {
            //Load all the Data that need to raise the event


            _busDuctCreationEventraiser.Raise();
        }

        private void RaiseElementSelectionEvent()
        {
            _selectionExternalEvent.Raise();
        }

        private void RaiseThemeChangeEvent()
        {
            // Change this line in the constructor:
            //ThemeChangeCommand = new RelayCommand(RaiseThemeChangeEvent);
           _externalEvent.Raise();
        }

        //PreLoading properties
       

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
