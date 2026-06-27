using Autodesk.Revit.UI;
using Multiversions.Revit.Sample.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Multiversions.Revit.Sample.ViewModels
{
    public class CommandViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        //Implement ICommand
        public ICommand ThemeChangeCommand { get; set; }

        private readonly TheThemeChanger _themeChangeEventHandler;
        private readonly ExternalEvent _externalEvent;


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetProperty<T>(ref T backingField, T value,
            [CallerMemberName] string propertyName = null)
        {
            if (Equals(backingField, value)) return false;
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public CommandViewModel()
        {            
            _themeChangeEventHandler = new TheThemeChanger();
            _externalEvent = ExternalEvent.Create(_themeChangeEventHandler);
            ThemeChangeCommand = new RelayCommand( RaiseThemeChangeEvent);
        }

        private void RaiseThemeChangeEvent()
        {
            // Change this line in the constructor:
            ThemeChangeCommand = new RelayCommand(RaiseThemeChangeEvent);
           _externalEvent.Raise();
        }
    }
}
