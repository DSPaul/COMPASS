using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace COMPASS.ViewModels.Commands
{
    //Command without parameters
    public class BasicCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        readonly Action _execute;

        public BasicCommand(Action Execute)
        {
            _execute = Execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute.Invoke();
        }
    }
}
