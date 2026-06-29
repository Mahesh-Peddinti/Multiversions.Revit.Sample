using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Models;
using System.ComponentModel;
using System.Windows.Input;

namespace Multiversions.Revit.Sample.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {        
        //Implement ICommand
        public ICommand ThemeChangeCommand { get; set; }

        private readonly TheThemeChanger _themeChangeEventHandler;
        private readonly ExternalEvent _externalEvent;       

        public MainViewModel()
        {            
            _themeChangeEventHandler = new TheThemeChanger();
            _externalEvent = ExternalEvent.Create(_themeChangeEventHandler);
            ThemeChangeCommand = new RelayCommand( RaiseThemeChangeEvent);
        }

        private void RaiseThemeChangeEvent()
        {
            // Change this line in the constructor:
            //ThemeChangeCommand = new RelayCommand(RaiseThemeChangeEvent);
           _externalEvent.Raise();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
