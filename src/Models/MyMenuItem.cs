using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace COMPASS.Models
{
    public class MyMenuItem : ObservableObject
    {
        public MyMenuItem(string header, Action<object> UpdateProp = null)
        {
            Header = header;
            _updateProp = UpdateProp;
        }

        #region Properties

        private Action<object> _updateProp;

        //Name of Item
        private string _header;
        public string Header
        {
            get { return _header; }
            set { SetProperty(ref _header, value); }
        }

        //Property it changes, such as bools for toggeling options, floats for sliders, ect.
        private object _prop;
        public object Prop
        {
            get { return _prop; }
            set 
            { 
                SetProperty(ref _prop, value);
                _updateProp?.Invoke(value);
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
