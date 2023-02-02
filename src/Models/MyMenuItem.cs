using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace COMPASS.Models
{
    public class MyMenuItem : ObservableObject
    {
        public MyMenuItem(string header, Func<object> getPropValue = null, Action<object> updateProp = null)
        {
            Header = header;
            _updateProp = updateProp;
            _getPropValue = getPropValue;

        }

        #region Properties

        private readonly Action<object> _updateProp;
        private readonly Func<object> _getPropValue;

        //Name of Item
        private string _header;
        public string Header
        {
            get { return _header; }
            set { SetProperty(ref _header, value); }
        }

        //Wrapper for Property because props cannot be passed by reference
        public object Prop
        {
            get
            {
                return _getPropValue?.Invoke();
            }

            set
            {
                _updateProp?.Invoke(value);
                RaisePropertyChanged(nameof(Prop));
            }
        }

        //Command to execute on click
        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetProperty(ref _command, value); }
        }

        private object _commandParam;
        public object CommandParam
        {
            get { return _commandParam; }
            set { SetProperty(ref _commandParam, value); }
        }

        private ObservableCollection<MyMenuItem> _submenus;
        public ObservableCollection<MyMenuItem> Submenus
        {
            get { return _submenus; }
            set { SetProperty(ref _submenus, value); }
        }
        #endregion
    }
}
